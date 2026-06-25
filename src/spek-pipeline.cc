#ifndef SPEK_CORE_NO_WX
#include <wx/intl.h>
#define ngettext wxPLURAL
#endif

#include <assert.h>
#include <math.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include <condition_variable>
#include <mutex>
#include <sstream>
#include <thread>
#include <vector>

#include "spek-audio.h"
#include "spek-fft.h"

#include "spek-pipeline.h"

enum
{
    NFFT = 64 // Number of FFTs to pre-fetch.
};

struct spek_pipeline
{
    std::unique_ptr<AudioFile> file;
    std::unique_ptr<FFTPlan> fft;
    int stream;
    int channel;
    enum window_function window_function;
    int samples;
    spek_pipeline_cb cb;
    void *cb_data;

    float *coss; // Pre-computed cos table.
    int nfft; // Size of the FFT transform.
    int input_size;
    int input_pos;
    float *input;
    float *output;

    std::thread reader_thread;
    bool has_reader_thread;
    std::mutex reader_mutex;
    std::condition_variable reader_cond;
    std::thread worker_thread;
    bool has_worker_thread;
    std::mutex worker_mutex;
    std::condition_variable worker_cond;
    bool worker_done;
    bool quit;
};

// Forward declarations.
static void * reader_func(void *);
static void * worker_func(void *);
static void reader_sync(struct spek_pipeline *p, int pos);

struct spek_pipeline * spek_pipeline_open(
    std::unique_ptr<AudioFile> file,
    std::unique_ptr<FFTPlan> fft,
    int stream,
    int channel,
    enum window_function window_function,
    int samples,
    spek_pipeline_cb cb,
    void *cb_data
)
{
    spek_pipeline *p = new spek_pipeline();
    p->file = std::move(file);
    p->fft = std::move(fft);
    p->stream = stream;
    p->channel = channel;
    p->window_function = window_function;
    p->samples = samples;
    p->cb = cb;
    p->cb_data = cb_data;

    p->coss = NULL;
    p->input = NULL;
    p->output = NULL;
    p->has_reader_thread = false;
    p->has_worker_thread = false;

    if (!p->file->get_error()) {
        p->nfft = p->fft->get_input_size();
        p->coss = (float*)malloc(p->nfft * sizeof(float));
        float cf = 2.0f * (float)M_PI / (p->nfft - 1.0f);
        for (int i = 0; i < p->nfft; ++i) {
            p->coss[i] = cosf(cf * i);
        }
        p->input_size = p->nfft * (NFFT * 2 + 1);
        p->input = (float*)malloc(p->input_size * sizeof(float));
        p->output = (float*)malloc(p->fft->get_output_size() * sizeof(float));
        p->file->start(channel, samples);
    }

    return p;
}

void spek_pipeline_start(struct spek_pipeline *p)
{
    if (!!p->file->get_error()) {
        return;
    }

    p->input_pos = 0;
    p->worker_done = false;
    p->quit = false;

    try {
        p->reader_thread = std::thread(reader_func, p);
        p->has_reader_thread = true;
    } catch (...) {
        spek_pipeline_close(p);
    }
}

void spek_pipeline_close(struct spek_pipeline *p)
{
    if (p->has_reader_thread) {
        p->quit = true;
        if (p->reader_thread.joinable()) {
            p->reader_thread.join();
        }
        p->has_reader_thread = false;
    }
    if (p->has_worker_thread) {
        {
            std::lock_guard<std::mutex> lock(p->worker_mutex);
            p->input_pos = -1;
        }
        p->worker_cond.notify_one();
        if (p->worker_thread.joinable()) {
            p->worker_thread.join();
        }
        p->has_worker_thread = false;
    }
    if (p->output) {
        free(p->output);
        p->output = NULL;
    }
    if (p->input) {
        free(p->input);
        p->input = NULL;
    }
    if (p->coss) {
        free(p->coss);
        p->coss = NULL;
    }

    p->file.reset();

    delete p;
}

std::string spek_pipeline_desc(const struct spek_pipeline *pipeline)
{
#ifdef SPEK_CORE_NO_WX
    std::vector<std::string> items;

    if (!pipeline->file->get_codec_name().empty()) {
        items.push_back(pipeline->file->get_codec_name());
    }

    if (pipeline->file->get_bit_rate()) {
        std::ostringstream item;
        item << (pipeline->file->get_bit_rate() + 500) / 1000 << " kbps";
        items.push_back(item.str());
    }

    if (pipeline->file->get_sample_rate()) {
        std::ostringstream item;
        item << pipeline->file->get_sample_rate() << " Hz";
        items.push_back(item.str());
    }

    if (pipeline->file->get_bits_per_sample() && !pipeline->file->get_bit_rate()) {
        std::ostringstream item;
        item << pipeline->file->get_bits_per_sample()
            << (pipeline->file->get_bits_per_sample() == 1 ? " bit" : " bits");
        items.push_back(item.str());
    }

    if (pipeline->file->get_channels()) {
        std::ostringstream item;
        item << "channel " << pipeline->channel + 1 << " / " << pipeline->file->get_channels();
        items.push_back(item.str());
    }

    if (pipeline->file->get_error() == AudioError::OK) {
        std::ostringstream fft_item;
        fft_item << "W:" << pipeline->nfft;
        items.push_back(fft_item.str());

        std::string window_function_name;
        switch (pipeline->window_function) {
        case WINDOW_HANN:
            window_function_name = "Hann";
            break;
        case WINDOW_HAMMING:
            window_function_name = "Hamming";
            break;
        case WINDOW_BLACKMAN_HARRIS:
            window_function_name = "Blackman-Harris";
            break;
        default:
            assert(false);
        }
        if (window_function_name.size()) {
            items.push_back("F:" + window_function_name);
        }
    }

    std::string desc;
    for (const auto& item : items) {
        if (!desc.empty()) {
            desc.append(", ");
        }
        desc.append(item);
    }

    std::string error;
    switch (pipeline->file->get_error()) {
    case AudioError::CANNOT_OPEN_FILE:
        error = "Cannot open input file";
        break;
    case AudioError::NO_STREAMS:
        error = "Cannot find stream info";
        break;
    case AudioError::NO_AUDIO:
        error = "The file contains no audio streams";
        break;
    case AudioError::NO_DECODER:
        error = "Cannot find decoder";
        break;
    case AudioError::NO_DURATION:
        error = "Unknown duration";
        break;
    case AudioError::NO_CHANNELS:
        error = "No audio channels";
        break;
    case AudioError::CANNOT_OPEN_DECODER:
        error = "Cannot open decoder";
        break;
    case AudioError::BAD_SAMPLE_FORMAT:
        error = "Unsupported sample format";
        break;
    case AudioError::OK:
        break;
    }

    if (desc.empty()) {
        desc = error;
    } else if (pipeline->stream < pipeline->file->get_streams()) {
        std::ostringstream item;
        item << "Stream " << pipeline->stream + 1 << " / "
            << pipeline->file->get_streams() << ": " << desc;
        desc = item.str();
    } else if (!error.empty()) {
        desc = error + ": " + desc;
    }

    return desc;
#else
    std::vector<std::string> items;

    if (!pipeline->file->get_codec_name().empty()) {
        items.push_back(pipeline->file->get_codec_name());
    }

    if (pipeline->file->get_bit_rate()) {
        items.push_back(std::string(
            wxString::Format(_("%d kbps"), (pipeline->file->get_bit_rate() + 500) / 1000).utf8_str()
        ));
    }

    if (pipeline->file->get_sample_rate()) {
        items.push_back(std::string(
            wxString::Format(_("%d Hz"), pipeline->file->get_sample_rate()).utf8_str()
        ));
    }

    // Include bits per sample only if there is no bitrate.
    if (pipeline->file->get_bits_per_sample() && !pipeline->file->get_bit_rate()) {
        items.push_back(std::string(
            wxString::Format(
                ngettext("%d bit", "%d bits", pipeline->file->get_bits_per_sample()),
                pipeline->file->get_bits_per_sample()
            ).utf8_str()
        ));
    }

    if (pipeline->file->get_channels()) {
        items.push_back(std::string(
            wxString::Format(
                // TRANSLATORS: first %d is the current channel, second %d is the total number.
                "channel %d / %d", pipeline->channel + 1, pipeline->file->get_channels()
            ).utf8_str()
        ));
    }

    if (pipeline->file->get_error() == AudioError::OK) {
        items.push_back(std::string(wxString::Format(wxT("W:%i"), pipeline->nfft).utf8_str()));

        std::string window_function_name;
        switch (pipeline->window_function) {
        case WINDOW_HANN:
            window_function_name = std::string("Hann");
            break;
        case WINDOW_HAMMING:
            window_function_name = std::string("Hamming");
            break;
        case WINDOW_BLACKMAN_HARRIS:
            window_function_name = std::string("Blackman–Harris");
            break;
        default:
            assert(false);
        }
        if (window_function_name.size()) {
            items.push_back("F:" + window_function_name);
        }
    }

    std::string desc;
    for (const auto& item : items) {
        if (!desc.empty()) {
            desc.append(", ");
        }
        desc.append(item);
    }

    wxString error;
    switch (pipeline->file->get_error()) {
    case AudioError::CANNOT_OPEN_FILE:
        error = _("Cannot open input file");
        break;
    case AudioError::NO_STREAMS:
        error = _("Cannot find stream info");
        break;
    case AudioError::NO_AUDIO:
        error = _("The file contains no audio streams");
        break;
    case AudioError::NO_DECODER:
        error = _("Cannot find decoder");
        break;
    case AudioError::NO_DURATION:
        error = _("Unknown duration");
        break;
    case AudioError::NO_CHANNELS:
        error = _("No audio channels");
        break;
    case AudioError::CANNOT_OPEN_DECODER:
        error = _("Cannot open decoder");
        break;
    case AudioError::BAD_SAMPLE_FORMAT:
        error = _("Unsupported sample format");
        break;
    case AudioError::OK:
        break;
    }

    auto error_string = std::string(error.utf8_str());
    if (desc.empty()) {
        desc = error_string;
    } else if (pipeline->stream < pipeline->file->get_streams()) {
        desc = std::string(
            wxString::Format(
                // TRANSLATORS: first %d is the stream number, second %d is the
                // total number of streams, %s is the stream description.
                _("Stream %d / %d: %s"),
                pipeline->stream + 1, pipeline->file->get_streams(), wxString::FromUTF8(desc.c_str())
            ).utf8_str()
        );
    } else if (!error_string.empty()) {
        desc = std::string(
            // TRANSLATORS: first %s is the error message, second %s is stream description.
            wxString::Format(_("%s: %s"), error_string.c_str(), desc.c_str()).utf8_str()
        );
    }

    return desc;
#endif
}

int spek_pipeline_streams(const struct spek_pipeline *pipeline)
{
    return pipeline->file->get_streams();
}

int spek_pipeline_channels(const struct spek_pipeline *pipeline)
{
    return pipeline->file->get_channels();
}

double spek_pipeline_duration(const struct spek_pipeline *pipeline)
{
    return pipeline->file->get_duration();
}

int spek_pipeline_sample_rate(const struct spek_pipeline *pipeline)
{
    return pipeline->file->get_sample_rate();
}

static void * reader_func(void *pp)
{
    struct spek_pipeline *p = (spek_pipeline*)pp;

    try {
        p->worker_thread = std::thread(worker_func, p);
        p->has_worker_thread = true;
    } catch (...) {
        return NULL;
    }

    int pos = 0, prev_pos = 0;
    int len;
    while ((len = p->file->read()) > 0) {
        if (p->quit) break;

        const float *buffer = p->file->get_buffer();
        while (len-- > 0) {
            p->input[pos] = *buffer++;
            pos = (pos + 1) % p->input_size;

            // Wake up the worker if we have enough data.
            if ((pos > prev_pos ? pos : pos + p->input_size) - prev_pos == p->nfft * NFFT) {
                reader_sync(p, prev_pos = pos);
            }
        }
        assert(len == -1);
    }

    if (pos != prev_pos) {
        // Process the remaining data.
        reader_sync(p, pos);
    }

    // Force the worker to quit.
    reader_sync(p, -1);
    if (p->worker_thread.joinable()) {
        p->worker_thread.join();
    }
    p->has_worker_thread = false;

    // Notify the client.
    p->cb(p->fft->get_output_size(), -1, NULL, p->cb_data);
    return NULL;
}

static void reader_sync(struct spek_pipeline *p, int pos)
{
    {
        std::unique_lock<std::mutex> lock(p->reader_mutex);
        p->reader_cond.wait(lock, [p] {
            return p->worker_done;
        });
        p->worker_done = false;
    }

    {
        std::lock_guard<std::mutex> lock(p->worker_mutex);
        p->input_pos = pos;
    }
    p->worker_cond.notify_one();
}

static float get_window(enum window_function f, int i, float *coss, int n) {
    switch (f) {
    case WINDOW_HANN:
        return 0.5f * (1.0f - coss[i]);
    case WINDOW_HAMMING:
        return 0.53836f - 0.46164f * coss[i];
    case WINDOW_BLACKMAN_HARRIS:
        return 0.35875f - 0.48829f * coss[i] + 0.14128f * coss[2*i % n] - 0.01168f * coss[3*i % n];
    default:
        assert(false);
        return 0.0f;
    }
}

static void * worker_func(void *pp)
{
    struct spek_pipeline *p = (spek_pipeline*)pp;

    int sample = 0;
    int64_t frames = 0;
    int64_t num_fft = 0;
    int64_t acc_error = 0;
    int head = 0, tail = 0;
    int prev_head = 0;

    memset(p->output, 0, sizeof(float) * p->fft->get_output_size());

    while (true) {
        {
            std::lock_guard<std::mutex> lock(p->reader_mutex);
            p->worker_done = true;
        }
        p->reader_cond.notify_one();

        {
            std::unique_lock<std::mutex> lock(p->worker_mutex);
            p->worker_cond.wait(lock, [p, tail] {
                return tail != p->input_pos;
            });
            tail = p->input_pos;
        }

        if (tail == -1) {
            return NULL;
        }

        while (true) {
            head = (head + 1) % p->input_size;
            if (head == tail) {
                head = prev_head;
                break;
            }
            frames++;

            // If we have enough frames for an FFT or we have
            // all frames required for the interval run and FFT.
            bool int_full =
                acc_error < p->file->get_error_base() &&
                frames == p->file->get_frames_per_interval();
            bool int_over =
                acc_error >= p->file->get_error_base() &&
                frames == 1 + p->file->get_frames_per_interval();

            if (frames % p->nfft == 0 || ((int_full || int_over) && num_fft == 0)) {
                prev_head = head;
                for (int i = 0; i < p->nfft; i++) {
                    float val = p->input[(p->input_size + head - p->nfft + i) % p->input_size];
                    val *= get_window(p->window_function, i, p->coss, p->nfft);
                    p->fft->set_input(i, val);
                }
                p->fft->execute();
                num_fft++;
                for (int i = 0; i < p->fft->get_output_size(); i++) {
                    p->output[i] += p->fft->get_output(i);
                }
            }

            // Do we have the FFTs for one interval?
            if (int_full || int_over) {
                if (int_over) {
                    acc_error -= p->file->get_error_base();
                } else {
                    acc_error += p->file->get_error_per_interval();
                }

                for (int i = 0; i < p->fft->get_output_size(); i++) {
                    p->output[i] /= num_fft;
                }

                if (sample == p->samples) break;
                p->cb(p->fft->get_output_size(), sample++, p->output, p->cb_data);

                memset(p->output, 0, sizeof(float) * p->fft->get_output_size());
                frames = 0;
                num_fft = 0;
            }
        }
    }
}
