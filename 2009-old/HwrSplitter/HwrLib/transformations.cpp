//+----------------------------------------------------------------------------+
//| Handwriting recognition - Image tools                                      |
//| Transformations                                                            |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include "image/pamImage.h"
#include "image/transformations.h"

using std::swap;

// ----------------------------------------------------------------------------- : Geometric transformations

PamImage<BWPixel> crop_to_ink(PamImage<BWPixel> const& im_in) {
	int width  = im_in.getWidth();
	int height = im_in.getHeight();
	PamImage<BWPixel>::pixels const& pixels();

	int top = 0;
	while (top < height) {
		for (int x = 0 ; x < width ; ++x) if (im_in.pix(x,top)) goto done_top;
		++top;
	}
done_top:

	int bottom = height - 1;
	while (bottom > 0) {
		for (int x = 0 ; x < width ; ++x) if (im_in.pix(x,bottom-1)) goto done_bottom;
		--bottom;
	}
done_bottom:

	int left = 0;
	while (left < width) {
		for (int y = 0 ; y < height ; ++y) if (im_in.pix(left,y)) goto done_left;
		++left;
	}
done_left:

	int right = width - 1;
	while (right > 0) {
		for (int y = 0 ; y < height ; ++y) if (im_in.pix(right-1,y)) goto done_right;
		--right;
	}
done_right:

	return crop(im_in, left, top, right, bottom);
}


PamImage<unsigned> CumulativeCount(PamImage<BWPixel> const& im){
	int w=im.getWidth()+1,h=im.getHeight()+1;
	PamImage<unsigned> out(w, h);
	out.pix(0,0)= 0;
	for(int y=0;y<h;++y) out.pix(0,y) = 0;
	for(int x=0;x<w;++x) out.pix(x,0) = 0;
	for(int y=1;y<h;++y) 
		for(int x=1;x<w;++x) 
			out.pix(x,y) = out.pix(x-1,y) + out.pix(x,y-1) - out.pix(x-1,y-1) + unsigned(im.pix(x-1,y-1)!=0);
	return out;
}

