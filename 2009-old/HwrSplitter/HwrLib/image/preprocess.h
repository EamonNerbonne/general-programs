//+----------------------------------------------------------------------------+
//| Handwriting recognition - Preprocessing                                    |
//| Preprocessing images by removing junk, etc.                                |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#ifndef PREPROCESS_H
#define PREPROCESS_H
#include "../HwrConfig.h"

// ----------------------------------------------------------------------------- : Includes

#include "pamImage.h"
#include "histograms.h"

// ----------------------------------------------------------------------------- : Preprocessing

PamImage<BWPixel> preprocess(PamImage<RGBPixel> const& input_image, float shear_angle);

PamImage<BWPixel> preprocessLimited(PamImage<RGBPixel> const& input_image);

PamImage<Float> featuresImage(ImageBW shearedImg, float shear, int &topXOffset);



// ----------------------------------------------------------------------------- : Utilities

///misc
void add_gaussian(std::vector<Float>& values, int mean, Float sigma2s, Float height);

/// Convolution:  values[i] = sum_j  values[i+j] * values[j]
/// values is treated as a circular array
void convolve(std::vector<Float>& values, const std::vector<Float>& window);

void blur_projection(std::vector<Float>& values, int radius);
Float mean(std::vector<Float> const & values);


// ----------------------------------------------------------------------------- : Pixel type conversion

/// Convert an image to greyscale by using the red channel
/// the red channel is chosen because we ignore the red margins in some images
PamImage<GrayPixel> red_channel(PamImage<RGBPixel> const im);

/// Threshold an image, values below the threshold become 1, above become 0
PamImage<BWPixel> threshold(PamImage<GrayPixel> const im, int threshold);

/// Invert a black and white image
PamImage<BWPixel> invert(PamImage<BWPixel> const im);



// ----------------------------------------------------------------------------- : Baseline
/// Find the baseline and xheight of a word
template<typename T> void find_baseline(PamImage<T> const& im, int& top_out, int& base_out) {
	// blur x projection
	vector<Float> projection;
	project_x(im, projection);
	blur_projection(projection, 3);
	// The base of the word is everything where the x projection is >= mean(x-projection), ...
	if (projection.empty()) return; // empty image, shouldn't happen
	Float mean = ::mean(projection);
	// ... and the largest contiguous segment
	int best_len = 0;
	int cur_top  = 0;
	for (int y = 0 ; y < (int)projection.size() ; ++y) {
		if (!(projection[y] > mean)) {
			int cur_len = y - cur_top;
			if (cur_len >= best_len) {
				best_len = cur_len;
				top_out  = cur_top;
				base_out = y;
			}
			cur_top = y + 1;
		}
	}
	// last?
	int cur_len = (int)projection.size() - cur_top;
	if (cur_len >= best_len) {
		best_len = cur_len;
		top_out  = cur_top;
		base_out = (int)projection.size();
	}
	// stay inside image, have a sensible size
	base_out = min((int)projection.size(), base_out);
	if (top_out + 2 >= base_out) {
		top_out  = (int)projection.size() / 2;
		base_out = (int)projection.size();
	}
}

// ----------------------------------------------------------------------------- : Letter breaks
/// Find the letter breaks of a word
template<typename T> void find_letters(PamImage<T> const& im, int top, int base, int minBreaks , std::vector<int>& out) {
	// some constants:
	const Float BORDER_EMPTY = 0.01f;  // below this we consider to be 0
	const Float BORDER_SIGMA = 120.0f;
	const Float BORDER_SCALE = 1.0f;
	const Float PLATEAU      = 0.04f;
	const Float INNER_STOP   = 0.4f;
	const Float INNER_SIGMA1 = 140.0f;
	const Float INNER_SCALE1 = 0.66f;
	const Float INNER_SIGMA2 = 648.0f;
	const Float INNER_SCALE2 = 0.13f;
	const Float INNER_SIGMA3 = 4.0f;
	const Float INNER_SCALE3 = 2.0f;
	
	// put the topline a bit higher
	//top = top * 2 / 3;
	
	// make a projection along the y axis.
	// both of the part inside the baseline and of the whole word
	vector<Float> projection, projection_base;
	project_y(im, projection);
	project_y(im, projection_base, top, base);
	
	// combine the two projections
	// the idea here is that if there is a large difference between the base and ascender/descender
	//  then it is likely that there is a letter here, since breaks occur on places
	//  where the ink touches the edges of the top/base line.
	for (size_t i = 0 ; i < projection.size() ; ++i) {
		projection[i] += fabs(projection_base[i] - projection[i]);
	}
	blur_projection(projection, 1);
	Float height = *std::max_element(projection.begin(), projection.end());
	
	// find the left edge
	for (size_t i = 0 ; i < projection.size() ; ++i) {
		if (projection[i] < height * BORDER_EMPTY) {
			projection[i] = height; // never find breaks here
		} else {
			projection[i] = height;
			out.push_back((int)i);
			add_gaussian(projection, (int)i-1, BORDER_SIGMA, height * BORDER_SCALE);
			break;
		}
	}
	
	// find the right edge
	for (size_t i = projection.size() - 1 ; i + 1 > 0 ; --i) {
		if (projection[i] < height * BORDER_EMPTY) {
			projection[i] = height; // never find breaks here
		} else {
			projection[i] = height;
			out.push_back((int)i);
			add_gaussian(projection, (int)i+1, BORDER_SIGMA, height * BORDER_SCALE);
			break;
		}
	}
	
	// now the interesting part: letter breaks in the interior of the word.
	while (true) {
		// find the lowest point in the projection
		size_t lowest = std::min_element(projection.begin(), projection.end()) - projection.begin();
		// stop if this minimum is too high
		if ((projection[lowest] >= height * INNER_STOP) && out.size()>=(size_t)minBreaks) break;
		// look around for a plateau
		size_t left = lowest, right = lowest;
		while (left > 0                      && projection[left -1] - PLATEAU * height < projection[lowest]) left  -= 1;
		while (right + 1 < projection.size() && projection[right+1] - PLATEAU * height < projection[lowest]) right += 1;
		// take the center
		int center = (int)(left + right)/2;
		out.push_back(center);
		// add some gaussians
		add_gaussian(projection, center, INNER_SIGMA1, height * INNER_SCALE1);
		add_gaussian(projection, center, INNER_SIGMA2, height * INNER_SCALE2);
		add_gaussian(projection, center, INNER_SIGMA3, height * INNER_SCALE3);
		projection[center]+=height;
	}
	
	// finally sort the output array
	sort(out.begin(), out.end());

	/*for(size_t i=1;i<out.size();i++) {
		if(out[i]==out[i-1]) {
			throw "No Go!";
		}
	}*/
}






// ----------------------------------------------------------------------------- : EOF
#endif
