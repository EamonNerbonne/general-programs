#pragma once
#include <random>

#ifdef _MSC_VER
#pragma warning(push) //don't warn about while(true)
#pragma warning(disable:4127) //don't warn about while(true)
#endif

template<typename arrayT>
void shuffle(std::mt19937& randGen, arrayT& arr, unsigned size) {
    for (unsigned i = 0; i < size;++i) {
        unsigned range = size - i;
        unsigned capped;
        while (true) {
            unsigned randNum = randGen();
            capped = randNum % range;
            // size-i might not divide 2^32 evenly; so reject rolls that are too high; they're too high if they're in the highest, incomplete bucket
            //if the bucket was incomplete; then I can't add range - 1 - capped without overflowing.
            // so if I overflow; try again
            if (randNum <= randNum + (range - 1 - capped)) break;
        }
        std::swap(arr[i], arr[i + capped]);
    }
}

#ifdef _MSC_VER
#pragma warning(pop) //don't warn about while(true)
#endif