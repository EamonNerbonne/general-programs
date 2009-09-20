#pragma once
#include <boost/scoped_array.hpp>
#include "SymbolClass.h"

struct AllSymbolClasses {
	int symbolCount;
	boost::scoped_array<SymbolClass> sym;
	int iteration;

	AllSymbolClasses(int symbolCount);


	inline SymbolClass & operator[](short symbol) {return sym[symbol];}
	inline SymbolClass const & operator[](short symbol) const {return sym[symbol];}
	inline SymbolClass & getSymbol(short symbol) {return sym[symbol];}
	inline SymbolClass const & getSymbol(short symbol) const {return sym[symbol];}
	inline short size() const {return symbolCount;}

	void initializeRandomly();
	void resetToZero();

	int AllocatedSize() const;
	void CombineWithDistributions(AllSymbolClasses const & other);
};
