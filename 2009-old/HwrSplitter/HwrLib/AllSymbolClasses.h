#pragma once
#include <boost/scoped_array.hpp>
#include "SymbolClass.h"

struct AllSymbolClasses {
	std::vector<SymbolClass> sym;
	int iteration;

	AllSymbolClasses(int symbolCount);


	inline SymbolClass & operator[](short symbol) {return sym[symbol];}
	inline SymbolClass const & operator[](short symbol) const {return sym[symbol];}
	inline SymbolClass & getSymbol(short symbol) {return sym[symbol];}
	inline SymbolClass const & getSymbol(short symbol) const {return sym[symbol];}
	inline short size() const {return (short)sym.size();}

	void initializeRandomly();
	void resetToZero();

	size_t AllocatedSize() const;
	void CombineWithDistributions(AllSymbolClasses const & other);
#if  DO_CHECK_CONSISTENCY
	inline int CheckConsistency() const {
		int errs =0 ;
		for(int i=0;i<size();i++)
			errs+= sym[i].CheckConsistency();
		return errs;
	}
#endif
};
