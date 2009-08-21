//+----------------------------------------------------------------------------+
//| Handwriting recognition - Utilities                                        |
//| lxj code conversion                                                        |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#pragma once
#ifndef LXJCONVERT_H
#define LXJCONVERT_H
#include "../stdafx.h"
// ----------------------------------------------------------------------------- : Includes


// ----------------------------------------------------------------------------- : StringTranslate

/// String translation table
class StringTranslate {
  private:
	std::map<wchar_t,std::wstring> map;
	std::set<wchar_t> range;
	
  public:
	StringTranslate(std::string filePath);
	
	//void Serialize(FILE* stream);
	
	std::wstring Translate(std::wstring word);
	std::wstring Range();
};

// ----------------------------------------------------------------------------- : EOF
#endif
