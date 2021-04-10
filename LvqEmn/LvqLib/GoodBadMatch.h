#pragma once
#include <numeric>
#include "utils.h"

struct MatchQuality {
    double distGood, distBad;
    double costFunc;
    double muJ, muK;
    //double lr;
    bool isErr;
};

struct GoodBadMatch {
    double distGood, distBad;
    int matchGood, matchBad;
    inline GoodBadMatch()
        : distGood(std::numeric_limits<double>::infinity())
        , distBad(std::numeric_limits<double>::infinity())
#ifndef NDEBUG
        , matchGood(-1)
        , matchBad(-1)
#endif
    {}

    MatchQuality LvqQuality();
    MatchQuality GgmQuality();

};


struct CorrectAndWorstMatches {
    struct MatchOk {
        double dist;
        int idx;
        MatchOk() {}
        MatchOk(double dist, int idx) :dist(dist), idx(idx) {}
        inline bool operator<(MatchOk const& other) const { return dist < other.dist; }
    };

    double distBad;
    int matchBad;
    MatchOk* matchesOk;
    int foundOk;

    inline CorrectAndWorstMatches(MatchOk* matchesOk)
        : distBad(std::numeric_limits<double>::infinity())
#ifndef NDEBUG
        , matchBad(-1)
#endif
        , matchesOk(matchesOk)
        , foundOk(0)
    {}

    inline void RegisterOk(double dist, int idx) { matchesOk[foundOk++] = MatchOk(dist, idx); }
    inline void RegisterBad(double dist, int idx) { if (!(dist >= distBad)) { matchBad = idx; distBad = dist; } }

    inline void Register(double dist, int idx, bool isOk) { if (isOk) RegisterOk(dist, idx); else RegisterBad(dist, idx); }

    inline void SortOk() { std::sort(matchesOk, matchesOk + foundOk); }

    GoodBadMatch ToGoodBadMatch() {
        GoodBadMatch retval;
        retval.distBad = distBad;
        retval.matchBad = matchBad;
        retval.distGood = matchesOk[0].dist;
        retval.matchGood = matchesOk[0].idx;
        assert(retval.matchGood >= 0);
        assert(retval.matchBad >= 0);
        return retval;
    }
};
