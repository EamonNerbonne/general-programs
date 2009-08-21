#ifndef SKELETON_H
#define SKELETON_H
#include "../stdafx.h"
#include "pamImage.h"
// ----------------------------------------------------------------------------- : Morpology

/// Morphological skeleton of an image
///  threshold = min difference in closest boundary point
///  fat       = don't force lines to be 1 pixel
PamImage<BWPixel> skeleton(PamImage<BWPixel> const& im, double threshold = 3.0, bool fat = false);

/// Distance transform of an image
PamImage<GrayPixel> distance_transform(PamImage<BWPixel> const& im);

#endif