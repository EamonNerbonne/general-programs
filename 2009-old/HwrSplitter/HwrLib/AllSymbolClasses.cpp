#include "StdAfx.h"
#include "AllSymbolClasses.h"

AllSymbolClasses::AllSymbolClasses(int symbolCount)	
	: symbolCount(symbolCount)
	, iteration(0)
	, sym(new SymbolClass[symbolCount])
{}

void  AllSymbolClasses::initializeRandomly() {
	for(int i=0;i<symbolCount;i++)
		sym[i].initializeRandomly();
}

void AllSymbolClasses::resetToZero() {
	for(int i=0;i<symbolCount;i++)
		sym[i].resetToZero();
	iteration = 0;
}

int AllSymbolClasses::AllocatedSize() const {return sizeof(AllSymbolClasses) + sizeof(SymbolClass)*symbolCount;}

void AllSymbolClasses::CombineWithDistributions(AllSymbolClasses const & other) {
	if(other.symbolCount!=this->symbolCount)
		throw "Error: SymbolCounts don't match!";
	for(int i=0;i<symbolCount;i++) {
		sym[i].CombineWithDistribution(other.sym[i]);
	}
	iteration += other.iteration;
}