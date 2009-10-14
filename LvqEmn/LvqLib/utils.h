#pragma once
#include "stdafx.h"

template <class T, class S> T sum(T zero, S & container) {	return std::accumulate(container.begin(),container.end(),zero);}