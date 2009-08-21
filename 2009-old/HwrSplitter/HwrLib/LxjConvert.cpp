//+----------------------------------------------------------------------------+
//| Handwriting recognition - Utilities                                        |
//| lxj code conversion                                                        |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include "util/LxjConvert.h"

#ifdef _MSC_VER
#pragma warning(disable:4996)
#endif

using namespace std;

// ----------------------------------------------------------------------------- : StringTranslate

//void StringTranslate::Serialize(FILE* stream){	throw "Not Implemented";}

StringTranslate::StringTranslate(string filePath) {
	FILE* stream = fopen(filePath.c_str(),"r, ccs=UTF-8");

	wchar_t* bufferC=0;
	wchar_t* buffer=0;
	try{
		bufferC = new wchar_t[4096];
		buffer = new wchar_t[4096];
	wstring replacement;
	int fieldsread=fwscanf(stream, L" %4095s %4095s ",bufferC,buffer);
	while(fieldsread==2) {
		wstring str = bufferC;
		wchar_t letter = (wchar_t)0;
		if(str.size()==1)
			letter=str[0];
		wstring mapsTo=buffer;
		map[letter]=mapsTo;
		for(int i=0;i<(int)mapsTo.size();i++)
			range.insert(mapsTo[i]);

		fieldsread=fwscanf(stream, L" %4095s %4095s ",bufferC,buffer);
	}
	}  catch(...) {
	delete[] bufferC;
	delete[] buffer;
throw;
	}
	delete[] bufferC;
	delete[] buffer;
}


wstring StringTranslate::Translate(wstring word) {
	wstring lxjword;
	for(int i=0;i<(int)word.size();i++) {
		wchar_t letter=word[i];
		if(map.find(letter)==map.end())
			letter= (wchar_t)0;
		lxjword+=map[letter];
	}
	return lxjword;
}

wstring StringTranslate::Range() {
	wstring retval;
	for(set<wchar_t>::const_iterator el=range.begin();el!=range.end();el++)
		retval+=*el;
	return retval;
}
