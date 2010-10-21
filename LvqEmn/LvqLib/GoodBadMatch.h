#pragma once
#include <numeric>

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
	double CostFunc() const { return (distGood - distBad)/(distGood+distBad); }
	bool IsErr()const{return distGood > distBad;}
};


struct CorrectAndWorstMatches {
	struct MatchOk {
		double dist;
		int idx;
		MatchOk()  {}
		MatchOk(double dist, int idx) :dist(dist),idx(idx) {}
		inline bool operator<(MatchOk const & other) const {return dist < other.dist;}
	};

	double distBad;
	int matchBad;
	MatchOk *matchesOk;
	int foundOk;

	inline CorrectAndWorstMatches(MatchOk*matchesOk)
		: distBad(std::numeric_limits<double>::infinity())
		, matchesOk(matchesOk)
		, foundOk(0)
#ifndef NDEBUG
		, matchGood(-1)
		, matchBad(-1)
#endif
	{}

	inline void RegisterOk(double dist,int idx) {
		matchesOk[foundOk++] = MatchOk(dist,idx);
	}
	inline void RegisterBad(double dist,int idx) {
		if(dist < distBad) {
			matchBad = idx;
			distBad = dist;
		}
	}

	inline void SortOk() { std::sort(matchesOk,matchesOk+foundOk); }

	double CostFunc() const { return (matchesOk[0].dist - distBad)/(matchesOk[0].dist+distBad); }
	bool IsErr()const{return matchesOk[0].dist > distBad;}

	GoodBadMatch ToGoodBadMatch() {
		GoodBadMatch retval;
		retval.distBad = distBad;
		retval.matchBad = matchBad;
		retval.distGood = matchesOk[0].dist;
		retval.matchGood = matchesOk[0].idx;
		return retval;
	}		
};
