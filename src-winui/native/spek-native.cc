#include "spek-native.h"

#include <algorithm>
#include <condition_variable>
#include <cstdlib>
#include <cstring>
#include <mutex>
#include <new>
#include <string>
#include <utility>
#include <vector>

#include "spek-audio.h"
#include "spek-fft.h"
#include "spek-palette.h"
#include "spek-pipeline.h"

namespace {

constexpr int RESULT_VERSION = 1;
constexpr int OPTIONS_VERSION = 1;
constexpr int MIN_FFT_BITS = 8;
constexpr int MAX_FFT_BITS = 14;
constexpr int DEFAULT_UPPER_RANGE = 0;
constexpr int DEFAULT_LOWER_RANGE = -120;

struct AnalyzeState {
    std::mutex mutex;
    std::condition_variable finished;
    bool done = false;
    int width = 0;
    int height = 0;
    int upper_range = DEFAULT_UPPER_RANGE;
    int lower_range = DEFAULT_LOWER_RANGE;
    palette palette_kind = PALETTE_DEFAULT;
    std::vector<uint8_t> pixels;
};

char *copy_string(const std::string& value)
{
    char *copy = static_cast<char*>(std::malloc(value.size() + 1));
    if (!copy) {
        return nullptr;
    }

    std::memcpy(copy, value.c_str(), value.size() + 1);
    return copy;
}

void set_error(spek_native_result *result, const std::string& message)
{
    if (result) {
        result->error = copy_string(message);
    }
}

void copy_bytes(uint8_t **target, const std::vector<uint8_t>& source)
{
    *target = static_cast<uint8_t*>(std::malloc(source.size()));
    if (*target && !source.empty()) {
        std::memcpy(*target, source.data(), source.size());
    }
}

int bits_to_bands(int bits)
{
    return (1 << (bits - 1)) + 1;
}

palette to_palette(int value)
{
    switch (value) {
    case SPEK_NATIVE_PALETTE_SPECTRUM:
        return PALETTE_SPECTRUM;
    case SPEK_NATIVE_PALETTE_MONO:
        return PALETTE_MONO;
    case SPEK_NATIVE_PALETTE_SOX:
    default:
        return PALETTE_SOX;
    }
}

window_function to_window_function(int value)
{
    switch (value) {
    case SPEK_NATIVE_WINDOW_HAMMING:
        return WINDOW_HAMMING;
    case SPEK_NATIVE_WINDOW_BLACKMAN_HARRIS:
        return WINDOW_BLACKMAN_HARRIS;
    case SPEK_NATIVE_WINDOW_HANN:
    default:
        return WINDOW_HANN;
    }
}

std::vector<uint8_t> create_palette_pixels(palette palette_kind, int width, int height)
{
    std::vector<uint8_t> pixels(width * height * 4);
    for (int y = 0; y < height; y++) {
        double level = 1.0 - y / static_cast<double>(std::max(1, height - 1));
        uint32_t color = spek_palette(palette_kind, level);
        for (int x = 0; x < width; x++) {
            int offset = (y * width + x) * 4;
            pixels[offset] = color & 0xFF;
            pixels[offset + 1] = (color >> 8) & 0xFF;
            pixels[offset + 2] = color >> 16;
            pixels[offset + 3] = 255;
        }
    }
    return pixels;
}

void pipeline_callback(int bands, int sample, float *values, void *cb_data)
{
    AnalyzeState *state = static_cast<AnalyzeState*>(cb_data);
    if (sample == -1) {
        {
            std::lock_guard<std::mutex> lock(state->mutex);
            state->done = true;
        }
        state->finished.notify_one();
        return;
    }

    if (sample < 0 || sample >= state->width || bands != state->height || !values) {
        return;
    }

    double range = state->upper_range - state->lower_range;
    for (int y = 0; y < bands; y++) {
        double value = std::min(
            static_cast<double>(state->upper_range),
            std::max(static_cast<double>(state->lower_range), static_cast<double>(values[y]))
        );
        double level = (value - state->lower_range) / range;
        uint32_t color = spek_palette(state->palette_kind, level);
        int target_y = bands - y - 1;
        int offset = (target_y * state->width + sample) * 4;
        state->pixels[offset] = color & 0xFF;
        state->pixels[offset + 1] = (color >> 8) & 0xFF;
        state->pixels[offset + 2] = color >> 16;
        state->pixels[offset + 3] = 255;
    }
}

} // namespace

extern "C" SPEK_NATIVE_EXPORT int32_t spek_native_analyze_utf8(
    const char *path,
    const spek_native_options *options,
    spek_native_result *result
)
{
    if (!path || !options || !result) {
        return 1;
    }

    std::memset(result, 0, sizeof(*result));
    result->version = RESULT_VERSION;

    if (options->version != OPTIONS_VERSION) {
        set_error(result, "Unsupported native analyzer options version.");
        return 2;
    }

    int fft_bits = std::max(MIN_FFT_BITS, std::min(MAX_FFT_BITS, options->fft_bits));
    int width = std::max(1, options->target_columns);
    int height = bits_to_bands(fft_bits);
    int stream = std::max(0, options->stream);
    int channel = std::max(0, options->channel);
    int upper_range = options->upper_range;
    int lower_range = options->lower_range;
    if (upper_range <= lower_range) {
        upper_range = DEFAULT_UPPER_RANGE;
        lower_range = DEFAULT_LOWER_RANGE;
    }

    palette palette_kind = to_palette(options->palette);
    window_function window_kind = to_window_function(options->window_function);

    try {
        AnalyzeState state;
        state.width = width;
        state.height = height;
        state.upper_range = upper_range;
        state.lower_range = lower_range;
        state.palette_kind = palette_kind;
        state.pixels.resize(width * height * 4);

        Audio audio;
        FFT fft;
        auto file = audio.open(path, stream);
        AudioError audio_error = file->get_error();
        int streams = file->get_streams();
        int channels = file->get_channels();
        double duration = file->get_duration();
        int sample_rate = file->get_sample_rate();
        if (channels > 0) {
            channel = std::min(channel, channels - 1);
        }

        spek_pipeline *pipeline = spek_pipeline_open(
            std::move(file),
            fft.create(fft_bits),
            stream,
            channel,
            window_kind,
            width,
            pipeline_callback,
            &state
        );

        std::string description = spek_pipeline_desc(pipeline);
        streams = spek_pipeline_streams(pipeline);
        channels = spek_pipeline_channels(pipeline);
        duration = spek_pipeline_duration(pipeline);
        sample_rate = spek_pipeline_sample_rate(pipeline);

        if (audio_error != AudioError::OK ||
            streams <= 0 || channels <= 0 || duration <= 0 || sample_rate <= 0) {
            spek_pipeline_close(pipeline);
            set_error(result, description.empty() ? "Cannot analyze audio file." : description);
            return 3;
        }

        spek_pipeline_start(pipeline);
        {
            std::unique_lock<std::mutex> lock(state.mutex);
            state.finished.wait(lock, [&state] {
                return state.done;
            });
        }
        spek_pipeline_close(pipeline);

        std::vector<uint8_t> palette_pixels = create_palette_pixels(palette_kind, 12, height);

        result->width = width;
        result->height = height;
        result->palette_width = 12;
        result->palette_height = height;
        result->duration_seconds = duration;
        result->sample_rate = sample_rate;
        result->streams = streams;
        result->stream = stream;
        result->channels = channels;
        result->channel = channel;
        result->fft_size = 1 << fft_bits;
        result->upper_range = upper_range;
        result->lower_range = lower_range;
        result->description = copy_string(description);
        copy_bytes(&result->pixels, state.pixels);
        copy_bytes(&result->palette_pixels, palette_pixels);

        if (!result->pixels || !result->palette_pixels || !result->description) {
            spek_native_free_result(result);
            set_error(result, "Out of memory.");
            return 4;
        }
    } catch (const std::exception& ex) {
        set_error(result, ex.what());
        return 5;
    } catch (...) {
        set_error(result, "Unknown native analyzer error.");
        return 6;
    }

    return 0;
}

extern "C" SPEK_NATIVE_EXPORT void spek_native_free_result(spek_native_result *result)
{
    if (!result) {
        return;
    }

    std::free(result->pixels);
    std::free(result->palette_pixels);
    std::free(result->description);
    std::free(result->error);
    std::memset(result, 0, sizeof(*result));
}
