#include "StdAfx.h"
#include "AllSymbolClasses.h"

AllSymbolClasses::AllSymbolClasses(int symbolCount)	
	:  iteration(0)
{
	sym.resize(symbolCount);
}

void  AllSymbolClasses::initializeRandomly() {
	for(int i=0;i<size();i++)
		sym[i].initializeRandomly();
}

void AllSymbolClasses::resetToZero() {
	for(int i=0;i<size();i++)
		sym[i].resetToZero();
	iteration = 0;
}

int AllSymbolClasses::AllocatedSize() const {return sizeof(AllSymbolClasses) + sizeof(SymbolClass)*sym.capacity();}

void AllSymbolClasses::CombineWithDistributions(AllSymbolClasses const & other) {
	if(other.size()!=this->size())
		throw "Error: SymbolCounts don't match!";
	for(int i=0;i<size();i++) {
		sym[i].CombineWithDistribution(other.sym[i]);
	}
	iteration += other.iteration;
}