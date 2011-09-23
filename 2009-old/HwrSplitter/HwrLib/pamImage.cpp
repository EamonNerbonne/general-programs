//+----------------------------------------------------------------------------+
//| Handwriting recognition - PamImage                                         |
//| Read and write binary .pbm, .pgm and .ppm files, manipulate images         |
//|                                                                            |
//| By Axel Brink and Tijn van der Zant                                        |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include "image/pamImage.h"
//#include <iostream>
//#include <cmath>
//#include <algorithm>

using namespace std;

#ifdef _MSC_VER
	//no buffer overflow warnings for fscanf:
	#pragma warning(disable : 4996)
	//return value ignored
	#pragma warning(disable : 6031)
	//conversion data truncation warning:
	#pragma warning(disable : 4244)
#endif

// ----------------------------------------------------------------------------- : Construction


RGBPixel getPixelRGB(PamImage<RGBPixel> const & im, float x, float y) {
  /* Uses linear interpolation to return an interpolated point.
   * Assumes that the image dimensions are [-0.5..width-0.5] x [-0.5..height-0.5]
   */
	int width = im.getWidth();
	//int height = im.getHeight();
    long x1, x2, y1, y2;
    float w1, w2, w3, w4; // weights:   w1 w2
                          //            w3 w4
    float xd, yd;         // distances
    float r, g, b, a;     // return values as floats
    RGBPixel retpixel;    // return value
    
    // determine neighboring pixels
    x1 = (long) x;
    x2 = x1 + 1;
    y1 = (long) y;
    y2 = y1 + 1;
    
    // determine relative position of interpolation point
    xd = x - (float) x1;
    yd = y - (float) y1;
    
    // determine pixel weights
    w1 = (1 - xd) * (1 - yd);
    w2 = xd * (1 - yd);
    w3 = (1 - xd) * yd;
    w4 = xd * yd;

    // prepare output pixel
    r = 0.0;
    g = 0.0;
    b = 0.0;
    a = 0.0;
    
    // apply weights for pixels inside the image
    if ((y1 >= 0) && (y1 < width)) {
        // top row
        if ((x1 >= 0) && (x1 < width)) {
            // top-left
            r += w1 * im.pix(x1,y1).r;
            g += w1 * im.pix(x1,y1).g;
            b += w1 * im.pix(x1,y1).b;
            a += w1 * im.pix(x1,y1).a;
        }
        if ((x2 >= 0) && (x2 < width)) {
            // top-right
            r += w2 * im.pix(x2,y1).r;
            g += w2 * im.pix(x2,y1).g;
            b += w2 * im.pix(x2,y1).b;
            a += w2 * im.pix(x2,y1).a;
        }
    }
    if ((y2 >= 0) && (y2 < width)) {
        // bottom row
        if ((x1 >= 0) && (x1 < width)) {
            // bottom-left
            r += w3 * im.pix(x1,y2).r;
            g += w3 * im.pix(x1,y2).g;
            b += w3 * im.pix(x1,y2).b;
            a += w3 * im.pix(x1,y2).a;
        }
        if ((x2 >= 0) && (x2 < width)) {
            // bottom-right
            r += w4 * im.pix(x2,y2).r;
            g += w4 * im.pix(x2,y2).g;
            b += w4 * im.pix(x2,y2).b;
            a += w4 * im.pix(x2,y2).a;
        }
    }
    retpixel.r = (unsigned char) r;
    retpixel.g = (unsigned char) g;
    retpixel.b = (unsigned char) b;
    retpixel.a = (unsigned char) a;
    
    return retpixel;
}

IntPixel get_minvalInt32(PamImage<IntPixel> const & im )  {
	IntPixel minV=im.pix(0,0);
	for(int y=0;y<im.getHeight();y++)
		for(int x=0;x<im.getWidth();x++) {
			IntPixel val=im.pix(x,y);
			if(val<minV)minV=val;
		}
	return minV;
}

IntPixel get_maxvalInt32(PamImage<IntPixel> const & im)  {
	IntPixel maxV=im.pix(0,0);
	for(int y=0;y<im.getHeight();y++)
		for(int x=0;x<im.getWidth();x++) {
			IntPixel val=im.pix(x,y);
			if(val>maxV)maxV=val;
		}
	return maxV;
}


// ----------------------------------------------------------------------------- : Conversion

