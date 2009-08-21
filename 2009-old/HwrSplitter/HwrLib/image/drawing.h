#ifndef DRAWING_H
#define DRAWING_H
#include "../HwrConfig.h"
#include "pamImage.h"
#include "../image/envelopes.h"
// ----------------------------------------------------------------------------- : Drawing

/// Draw a line in a grayscale image
void line_gray (PamImage<GrayPixel>& im, int x1, int y1, int x2, int y2, GrayPixel grayval);

/// Draw a rectangle in a grayscale image
void block_gray(PamImage<GrayPixel>& im, int x1, int y1, int x2, int y2, GrayPixel grayval);

/// Draw a rectangle in an rgb image
void block_rgb(PamImage<RGBPixel>& im, int x1, int y1, int x2, int y2, RGBPixel color, float alpha);

/// Draw a horizontal line
void horizontal_line_rgb(PamImage<RGBPixel>& im, int y, RGBPixel color, float alpha = 0.7);

/// Draw a x-projection histogram
void draw_x_projection(PamImage<RGBPixel>& im, int x0, int x1, const std::vector<Float>& histo, RGBPixel color, float alpha = 0.7);
/// Draw a x-projection histogram
void draw_y_projection(PamImage<RGBPixel>& im, int y0, int y1, const std::vector<Float>& histo, RGBPixel color, float alpha = 0.7);

/// Draw a boundary
void draw_boundary(PamImage<RGBPixel>& im, const Boundary& b);

#endif