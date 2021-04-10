#pragma once
#include <xmmintrin.h>

static void EIGEN_STRONG_INLINE prefetch(void const* start, int lines) {
    for (int i = 0;i < lines;i++)
        _mm_prefetch((const char*)start + 64 * i, _MM_HINT_T0);//_MM_HINT_T0 or _MM_HINT_NTA
}

static void EIGEN_STRONG_INLINE prefetchStream(void const* start, int lines) {
    for (int i = 0;i < lines;i++)
        _mm_prefetch((const char*)start + 64 * i, _MM_HINT_NTA);//_MM_HINT_T0 or _MM_HINT_NTA
}
