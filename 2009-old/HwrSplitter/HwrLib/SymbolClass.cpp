#include "StdAfx.h"
#include "SymbolClass.h"
void SymbolClass::initRandom()
{
	for(int i=0;i<SUB_SYMBOL_COUNT;i++)
		state[i].initRandom();
}
