#pragma once

#include <stdint.h>

#if defined(_WIN32)
#define SPEK_NATIVE_EXPORT __declspec(dllexport)
#else
#define SPEK_NATIVE_EXPORT
#endif

extern "C" {

enum spek_native_palette {
    SPEK_NATIVE_PALETTE_SPECTRUM = 0,
    SPEK_NATIVE_PALETTE_SOX = 1,
    SPEK_NATIVE_PALETTE_MONO = 2
};

enum spek_native_window_function {
    SPEK_NATIVE_WINDOW_HANN = 0,
    SPEK_NATIVE_WINDOW_HAMMING = 1,
    SPEK_NATIVE_WINDOW_BLACKMAN_HARRIS = 2
};

struct spek_native_options {
    int32_t version;
    int32_t target_columns;
    int32_t stream;
    int32_t channel;
    int32_t fft_bits;
    int32_t upper_range;
    int32_t lower_range;
    int32_t palette;
    int32_t window_function;
};

struct spek_native_result {
    int32_t version;
    int32_t width;
    int32_t height;
    uint8_t *pixels;
    int32_t palette_width;
    int32_t palette_height;
    uint8_t *palette_pixels;
    double duration_seconds;
    int32_t sample_rate;
    int32_t streams;
    int32_t stream;
    int32_t channels;
    int32_t channel;
    int32_t fft_size;
    int32_t upper_range;
    int32_t lower_range;
    char *description;
    char *error;
};

SPEK_NATIVE_EXPORT int32_t spek_native_analyze_utf8(
    const char *path,
    const spek_native_options *options,
    spek_native_result *result
);

SPEK_NATIVE_EXPORT void spek_native_free_result(spek_native_result *result);

}
