#ifndef HISTOGRAMS_H
#define HISTOGRAMS_H
#include "../HwrConfig.h"
#include "pamImage.h"

// ----------------------------------------------------------------------------- : Histogram like functions

/// Project the values in an image along the x axis
/// the vector will have one element for each row in the image
template<typename T> void project_x(PamImage<T> const& im, std::vector<Float>& out, int x1 = 0, int x2 = INT_MAX);

/// Project the values in an image along the y axis
/// the vector will have one element for each column in the image
template<typename T> void project_y(PamImage<T> const& im, std::vector<Float>& out, int y1 = 0, int y2 = INT_MAX);

/// Histogram of pixel values with the given number of bins
template<typename T> void histogram(PamImage<T> const& im, std::vector<Float>& out, int bins = 16);


// Project the values in an image along the x axis
template<typename T> void project_x(PamImage<T> const& im, std::vector<Float>& rows, int x1, int x2) {
	rows.clear();
	int width  = im.getWidth();
	int height = im.getHeight();
	x1 = max(0, min(width, x1));
	x2 = max(0, min(width, x2));
	Float overall = 1e-100f;
	for (int y = 0; y < height; y++) {
		Float total = 0;
		for (int x = x1; x < x2; x++) {
			total += to_gray(im.pix(x,y));
		}
		overall += total;
		rows.push_back(total);
	}
	for (int y = 0; y < height; y++) {
		rows[y] /= overall;
	}
}

// Project the values in an image along the y axis
template<typename T> void project_y(PamImage<T> const& im, std::vector<Float>& cols, int y1, int y2) {
	cols.clear();
	int width  = im.getWidth();
	int height = im.getHeight();
	y1 = max(0, min(height, y1));
	y2 = max(0, min(height, y2));
	Float overall = 1e-100;
	for (int x = 0; x < width; x++) {
		Float total = 0;
		for (int y = y1; y < y2; y++) {
			total += to_gray(im.pix(x,y));
		}
		overall += total;
		cols.push_back(total);
	}
	for (int x = 0; x < width; x++) {
		cols[x] /= overall;
	}
}

template<typename T> void histogram(PamImage<T> const&im, std::vector<Float>& histo, int bins) {
	bins = max(1, min(bins, 256));
	histo.clear(); histo.resize(bins, 0.);
	long width  = im.getWidth();
	long height = im.getHeight();
	Float delta = 1.0f / (width * height);
	for (long y = 0; y < height; y++) {
		for (long x = 0; x < width; x++) {
			int bin = (to_gray(im.pix(x,y)) * bins + 128) / 255;
			histo[bin] += delta;
		}
	}
}


#endif