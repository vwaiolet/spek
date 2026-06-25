#include <cmath>

#ifdef SPEK_CORE_NO_WX
#include <algorithm>
#include <vector>
#else
#define __STDC_CONSTANT_MACROS
extern "C" {
#include <libavcodec/avfft.h>
}
#endif

#include "spek-fft.h"

class FFTPlanImpl : public FFTPlan
{
public:
    FFTPlanImpl(int nbits);
#ifndef SPEK_CORE_NO_WX
    ~FFTPlanImpl() override;
#endif

    void execute() override;

private:
#ifdef SPEK_CORE_NO_WX
    std::vector<double> real;
    std::vector<double> imaginary;
#else
    struct RDFTContext *cx;
#endif
};

std::unique_ptr<FFTPlan> FFT::create(int nbits)
{
    return std::unique_ptr<FFTPlan>(new FFTPlanImpl(nbits));
}

#ifdef SPEK_CORE_NO_WX

FFTPlanImpl::FFTPlanImpl(int nbits) :
    FFTPlan(nbits),
    real(this->get_input_size()),
    imaginary(this->get_input_size())
{
}

static void fft(std::vector<double>& real, std::vector<double>& imaginary)
{
    int n = (int)real.size();
    for (int i = 1, j = 0; i < n; i++) {
        int bit = n >> 1;
        for (; j & bit; bit >>= 1) {
            j ^= bit;
        }
        j ^= bit;

        if (i < j) {
            std::swap(real[i], real[j]);
            std::swap(imaginary[i], imaginary[j]);
        }
    }

    for (int length = 2; length <= n; length <<= 1) {
        double angle = -2.0 * std::acos(-1.0) / length;
        double w_length_real = std::cos(angle);
        double w_length_imaginary = std::sin(angle);

        for (int i = 0; i < n; i += length) {
            double w_real = 1.0;
            double w_imaginary = 0.0;

            for (int j = 0; j < length / 2; j++) {
                int even = i + j;
                int odd = even + length / 2;

                double odd_real = real[odd] * w_real - imaginary[odd] * w_imaginary;
                double odd_imaginary = real[odd] * w_imaginary + imaginary[odd] * w_real;

                real[odd] = real[even] - odd_real;
                imaginary[odd] = imaginary[even] - odd_imaginary;
                real[even] += odd_real;
                imaginary[even] += odd_imaginary;

                double next_real = w_real * w_length_real - w_imaginary * w_length_imaginary;
                w_imaginary = w_real * w_length_imaginary + w_imaginary * w_length_real;
                w_real = next_real;
            }
        }
    }
}

void FFTPlanImpl::execute()
{
    int n = this->get_input_size();
    double n2 = n * (double)n;
    for (int i = 0; i < n; i++) {
        this->real[i] = this->get_input(i);
        this->imaginary[i] = 0.0;
    }

    fft(this->real, this->imaginary);

    for (int i = 0; i <= n / 2; i++) {
        double magnitude = (this->real[i] * this->real[i] + this->imaginary[i] * this->imaginary[i]) / n2;
        this->set_output(i, (float)(10.0 * std::log10(std::max(magnitude, 1.0e-20))));
    }
}

#else

FFTPlanImpl::FFTPlanImpl(int nbits) : FFTPlan(nbits), cx(av_rdft_init(nbits, DFT_R2C))
{
}

FFTPlanImpl::~FFTPlanImpl()
{
    av_rdft_end(this->cx);
}

void FFTPlanImpl::execute()
{
    av_rdft_calc(this->cx, this->get_input());

    // Calculate magnitudes.
    int n = this->get_input_size();
    float n2 = n * n;
    this->set_output(0, 10.0f * log10f(this->get_input(0) * this->get_input(0) / n2));
    this->set_output(n / 2, 10.0f * log10f(this->get_input(1) * this->get_input(1) / n2));
    for (int i = 1; i < n / 2; i++) {
        float re = this->get_input(i * 2);
        float im = this->get_input(i * 2 + 1);
        this->set_output(i, 10.0f * log10f((re * re + im * im) / n2));
    }
}

#endif
