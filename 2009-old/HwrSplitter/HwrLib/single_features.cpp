//+----------------------------------------------------------------------------+
//| Handwriting recognition - Features                                         |
//| Feature extraction from images, single function per feature                |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes

#include "stdafx.h"
#include "feature/features.h"

using namespace std;

#ifndef ASSERT
#define ASSERT(a)
#endif

// ----------------------------------------------------------------------------- : Features

double dark_fraction(PamImage<BWPixel> const& im, int x0, int y0, int x1, int y1) {
    x0 = max(x0, 0);
    y0 = max(y0, 0);
    x1 = min(x1, im.getWidth());
    y1 = min(y1, im.getHeight());
    
    int count = 0;
    for (long y = y0 ; y < y1 ; y++) {
        for (long x = x0 ; x < x1 ; x++) {
            if (im.pix(x,y)) count++;
        }
    }
    
    return (double)count / max(1, (x1-x0) * (y1-y0));
}

// ----------------------------------------------------------------------------- : Angles

floats angle_types(PamImage<BWPixel> const& im, int x0, int y0, int width, int height) {
    floats result;
    angle_types(im, x0, y0, width, height, &*result.begin());
    return result;
}

void angle_types(PamImage<BWPixel> const& im, int x0, int y0, int width, int height, double* out) {
    x0     = max(x0, 0);
    y0     = max(y0, 0);
    width  = min(width,  im.getWidth());
    height = min(height, im.getHeight());
    
    // all combination of four corners
    //   TL TR
    //   BL BR
    const int TL = 1, TR = 2, BL = 4, BR = 8, ALL = 15;
    int types[16] = {0};
    
    for (long y = y0 ; y + 1 < height ; y++) {
        for (long x = x0 ; x + 1 < width ; x++) {
            int type = (im.pix(x  ,y  ) ? TL : 0)
                     | (im.pix(x+1,y  ) ? TR : 0)
                     | (im.pix(x  ,y+1) ? BL : 0)
                     | (im.pix(x+1,y+1) ? BR : 0);
            types[type]++;
        }
    }
    
    //double total = max(1, width * height);
    double total = (double)( types[1]+types[2]+types[3] +types[4] +types[5] +types[6] +types[7]
                             + types[8]+types[9]+types[10]+types[11]+types[12]+types[13]+types[14]);
    out[0] = (types[TL+TR] + types[BL+BR])      / total; // "-" 0 degrees
    out[1] = (types[TL] + types[BR] + types[TR+BL]
               + types[ALL-TL] + types[ALL-BR]) / total; // "/" 45 degrees
    out[2] = (types[TL+BL] + types[TR+BR])      / total; // "|" 90 degrees
    out[3] = (types[TR] + types[BL] + types[TL+BR]
               + types[ALL-TR] + types[ALL-BL]) / total; // "\" 135 degrees
    
    bool verbose = false;
    if (verbose) {
        cerr << endl;
        for (int i = 0 ; i < 16 ; ++i) {
            cerr << (i & TL ? "TL " : "   ");
            cerr << (i & TR ? "TR " : "   ");
            cerr << (i & BL ? "BL " : "   ");
            cerr << (i & BR ? "BR " : "   ");
            cerr << std::setw(5) << types[i] << endl;
        }
    }
}

// ----------------------------------------------------------------------------- : Statistics

floats mean_variance(PamImage<BWPixel> const& im, int x0, int y0, int width, int height) {
    x0     = max(x0, 0);
    y0     = max(y0, 0);
    width  = min(width,  im.getWidth());
    height = min(height, im.getHeight());
    
    double tot_x = 0, tot_y = 0, tot_xx = 0, tot_yy = 0;
    int count = 0;
    
    for (long y = y0 ; y < height ; y++) {
        for (long x = x0 ; x < width ; x++) {
            if (im.pix(x,y)) {
                tot_x += (double)x / width;
                tot_y += (double)y / height;
                tot_xx += sqr((double)x / width);
                tot_yy += sqr((double)y / height);
                count++;
            }
        }
    }
    
    floats result;
    result.push_back(tot_x / count);
    result.push_back(tot_y / count);
    result.push_back(tot_xx / count - sqr(tot_x / count));
    result.push_back(tot_yy / count - sqr(tot_y / count));
    return result;
}

// ----------------------------------------------------------------------------- : Floodfill

void floodfill(int x, int y, PamImage<BWPixel> & im) {//TODO: isn't this a redundant copy?
    if (im.pix(x,y)) return;
	int width = im.getWidth();
	int height = im.getHeight();
    long l = x, r = x; // l = lowest x to fill, r = after largest x to fill
    while (l - 1 >= 0 && !im.pix(l-1,y)) l--;
    while (r < width  && !im.pix(r,y))   r++;
    for (long x2 = l ; x2 < r ; ++x2) {
        im.pix(x2,y) = 1;
    }
    for (long x2 = l ; x2 < r ; ++x2) {
        if (y - 1 >= 0)     floodfill(x2, y-1, im);
        if (y + 1 < height) floodfill(x2, y+1, im);
    }
}

void floodfill_border(PamImage<BWPixel> & im) {
    // top/bottom
	for (int x = 0 ; x < im.getWidth() ; ++x) {
        floodfill(x, 0,        im);
		floodfill(x, im.getHeight()-1, im);
    }
    // left/right
	for (int y = 0 ; y < im.getHeight() ; ++y) {
        floodfill(0,       y, im);
		floodfill(im.getWidth()-1, y, im);
    }
}

void floodfill_corners(PamImage<BWPixel> & im) {
    // top/bottom
    floodfill(0,       0,        im);
    floodfill(0,       im.getHeight()-1, im);
    floodfill(im.getWidth()-1, 0,        im);
    floodfill(im.getWidth()-1, im.getHeight()-1, im);
}
