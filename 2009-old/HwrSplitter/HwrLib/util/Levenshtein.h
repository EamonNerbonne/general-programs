//+----------------------------------------------------------------------------+
//| Handwriting recognition - Utilities                                        |
//| Levenshtein distance                                                       |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#pragma once
#ifndef LEVENSHTEIN_H
#define LEVENSHTEIN_H
#include "../stdafx.h"


// ----------------------------------------------------------------------------- : Levenshtein distance

namespace levenshtein {
int distance(const std::wstring source, const std::wstring target);
}

// ----------------------------------------------------------------------------- : EOF
#endif
