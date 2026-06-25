cmake_minimum_required(VERSION 3.24)

project(spek_native LANGUAGES CXX)

add_library(spek-native SHARED
    spek-native.cc
    ../../src/spek-audio.cc
    ../../src/spek-fft.cc
    ../../src/spek-palette.cc
    ../../src/spek-pipeline.cc
)

set_target_properties(spek-native PROPERTIES
    PREFIX ""
    OUTPUT_NAME "spek-native"
)

target_compile_features(spek-native PRIVATE cxx_std_17)

target_compile_definitions(spek-native PRIVATE
    SPEK_CORE_NO_WX
    PACKAGE_NAME="Spek"
    PACKAGE_VERSION="0.8.5"
    _USE_MATH_DEFINES
)

target_include_directories(spek-native PRIVATE
    .
    ../../src
)

find_package(PkgConfig QUIET)
if (PkgConfig_FOUND)
    pkg_check_modules(FFMPEG REQUIRED IMPORTED_TARGET
        libavformat
        libavcodec
        libavutil
    )
    target_link_libraries(spek-native PRIVATE PkgConfig::FFMPEG)
else()
    message(FATAL_ERROR "PkgConfig is required to locate FFmpeg development libraries.")
endif()

if (WIN32)
    target_compile_definitions(spek-native PRIVATE
        NOMINMAX
        WIN32_LEAN_AND_MEAN
    )
endif()
