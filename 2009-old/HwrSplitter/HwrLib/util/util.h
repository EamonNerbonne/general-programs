//+----------------------------------------------------------------------------+
//| Handwriting recognition - Utilities                                        |
//| String conversion and other utilities                                      |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#ifndef UTIL_H
#define UTIL_H

// ----------------------------------------------------------------------------- : Includes
#include "../stdafx.h"

// ----------------------------------------------------------------------------- : String conversion

inline char to_char(wchar_t c) {
	return (char)c;
}
inline wchar_t to_wchar(char c) {
	return (wchar_t)c;
}

inline std::string to_string(const std::wstring& w) {
	std::string s(w.size(), ' ');
	std::transform(w.begin(), w.end(), s.begin(), to_char);
	return s;
}
inline std::wstring to_wstring(const std::string& s) {
	std::wstring w(s.size(), ' ');
	std::transform(s.begin(), s.end(), w.begin(), to_wchar);
	return w;
}

// ----------------------------------------------------------------------------- : EOF
#endif
