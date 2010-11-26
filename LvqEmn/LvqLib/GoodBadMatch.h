#pragma once
#include <numeric>
#include "utils.h"


struct MatchQuality {
	double costFunc;
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

	bool IsErr()const{return distGood > distBad;}

	MatchQuality LvqQuality() {
		MatchQuality retval;
		retval.isErr = distGood >= distBad;
		retval.costFunc = (distGood - distBad)/(distGood+distBad);
		return retval;
	}

	MatchQuality GmmQuality() {
		MatchQuality retval;
		retval.isErr = distGood >= distBad;
		if(retval.isErr) {
			double pk_pj = exp(distGood-distBad); //0 -> 1
			retval.costFunc = 2*pk_pj/(1+pk_pj)-1;
		} else {
			double pj_pk = exp(distBad-distGood); //0 -> 1
			retval.costFunc = 2/(1+pj_pk)-1;
		}
		if(!isfinite(retval.costFunc)) throw "Invalid Cost func!";
		return retval;
	}

	double MuJ() const {return -2.0*distGood / (sqr(distGood) + sqr(distBad));}
	double MuK() const{return +2.0*distBad / (sqr(distGood) + sqr(distBad));}
};


struct CorrectAndWorstMatches {
	struct MatchOk {
		double dist;
		int idx;
		MatchOk() {}
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
		, matchBad(-1)
#endif
	{}

	inline void RegisterOk(double dist,int idx) { matchesOk[foundOk++] = MatchOk(dist,idx); }
	inline void RegisterBad(double dist,int idx) { if(dist < distBad) { matchBad = idx; distBad = dist; } }

	inline void Register(double dist,int idx, bool isOk) { if(isOk) RegisterOk(dist,idx); else RegisterBad(dist,idx); }

	inline void SortOk() { std::sort(matchesOk,matchesOk+foundOk); }

	//double CostFunc() const { return (matchesOk[0].dist - distBad)/(matchesOk[0].dist+distBad); }
	//double MuJ() const {return -2.0*matchesOk[0].dist / (sqr(matchesOk[0].dist) + sqr(distBad));}
	//double MuK() const{return +2.0*distBad / (sqr(matchesOk[0].dist) + sqr(distBad));}
	//bool IsErr()const{return matchesOk[0].dist > distBad;}

	GoodBadMatch ToGoodBadMatch() {
		GoodBadMatch retval;
		retval.distBad = distBad;
		retval.matchBad = matchBad;
		retval.distGood = matchesOk[0].dist;
		retval.matchGood = matchesOk[0].idx;
		return retval;
	}		
};
