//+----------------------------------------------------------------------------+
//| Handwriting recognition - Preprocessing                                    |
//| Preprocessing images by removing junk, etc.                                |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#ifndef TRANSFORMATIONS_H
#define TRANSFORMATIONS_H
#include "../HwrConfig.h"

// ----------------------------------------------------------------------------- : Geometric transformations

/// Crop an image to the least rectangle containg (left,top) and (right,bottom)
template<typename T> PamImage<T> crop(PamImage<T> const& im_in, long left, long top, long right, long bottom);

/// Crop an image to the least rectangle containg all ink
PamImage<BWPixel> crop_to_ink(PamImage<BWPixel> const& im_in);

/// Unshear an image by some factor
template<typename T> PamImage<T> unshear_factor(PamImage<T> const& im_in, float factor);
/// Unshear an image by some angle
template<typename T> PamImage<T> unshear(PamImage<T> const& im_in, float angle);


// ----------------------------------------------------------------------------- : template implementaions
// crop an image
template<typename T> PamImage<T> crop(PamImage<T> const& im_in, long left, long top, long right, long bottom) {
	long width  = im_in.getWidth();
	long height = im_in.getHeight();

	// try to transform the rectangle such that it is inside the image 
	if (left > right) swap(left, right);
	if (top > bottom) swap(top, bottom);
	if (left < 0) left = 0;
	if (right > width - 1) right = width - 1;
	if (top < 0) top = 0;
	if (bottom > height - 1) bottom = height - 1;

	if ((right < 0) || (left > width - 1) || (top > height - 1) || (bottom < 0)) {
		return PamImage<T>(0, 0);
	} else {
		// cropping rectangle is ok
		long cropwidth = right - left + 1;
		long cropheight = bottom - top + 1;

		PamImage<T> im_out(cropwidth, cropheight);

		for (long y = 0; y < cropheight; ++y) {
			for( long x =0;x<cropwidth;x++) {
				im_out.pix(x,y)=im_in.pix(x+left,y+top);
			}
		}

		return im_out;
	}
}


// unshear an image, crops areas outside the parallelogram
template<typename T> PamImage<T> unshear_factor(PamImage<T> const& im_in, float factor) {
	long width     = im_in.getWidth();
	long height    = im_in.getHeight();
	long new_width = (long)(width - factor * (height-1));

	if (new_width > width) {
		std::cerr << "Error: can't unshear image, size is " << width << "*" << height << std::endl;
		throw 1;
		//        return NULL;
	}

	PamImage<T> im_out(new_width, height);
	for (long y = 0; y < height; ++y) {
		long dx = (long)((height - 1 - y ) * factor);
		for(long x=0;x<new_width;x++) {
			im_out.pix(x,y) = im_in.pix(x+dx,y);//upto nW-1+h-1==w-h+1-1+h-1==w-2
		}
	}

	return im_out;
}

template<typename T> PamImage<T> unshear(PamImage<T> const& im_in, float angle) {
	return unshear_factor(im_in, (float)tan(angle * M_PI / 180));
}


PamImage<unsigned> CumulativeCount(PamImage<BWPixel> const& im);
inline unsigned SumInRect(PamImage<unsigned> const &sumImg,int x0,int x1,int y0,int y1) {
	return sumImg.pix(x1,y1) + sumImg.pix(x0,y0) - sumImg.pix(x0,y1) - sumImg.pix(x1,y0);
}
#endif