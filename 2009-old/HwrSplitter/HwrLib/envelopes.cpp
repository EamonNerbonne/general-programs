//+----------------------------------------------------------------------------+
//| Handwriting recognition - Image tools                                      |
//| Properties of images - Envelopes and boundaries                            |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include "image/envelopes.h"

using namespace std;

// ----------------------------------------------------------------------------- : Envelopes


// 'blur' an envelope by taking the minimum/maximum over a certain width
std::vector<int> blur_envelope(std::vector<int> envelope, bool upper, int width) {
	std::vector<int> new_envelope;
	for (int i = 0 ; i < (int)envelope.size() ; ++i) {
		int v = envelope[i];
		for (int x = max(0, i - width/2) ; x < min((int)envelope.size(), i + width / 2) ; ++x) {
			v = upper ? min(v, envelope[x]) : max(v, envelope[x]);
		}
		new_envelope.push_back(v);
	}
	return new_envelope;
}

// ----------------------------------------------------------------------------- : Facing

std::vector<int> up_facing(PamImage<BWPixel> const& im) {
    long width  = im.getWidth();
    long height = im.getHeight();
    std::vector<int> ys;
    for (long y = 0 ; y < height - 1 ; y++) {
        for (long x = 0 ; x < width ; x++) {
            // white pixel, dark below
            //if (!im.pix(x,y) && im.pix(x,y+1)) {
            // white pixel, dark below, but not to the sides
            if (!im.pix(x,y) && im.pix(x,y+1) && (x == 0 || !im.pix(x-1,y)) && (x == width-1 || !im.pix(x+1,y))) {
                ys.push_back(y);
            }
        }
    }
    std::sort(ys.begin(), ys.end());
    return ys;
}

std::vector<int> down_facing(PamImage<BWPixel> const& im) {
    long width  = im.getWidth();
    long height = im.getHeight();
    std::vector<int> ys;
    for (long y = 1 ; y < height ; y++) {
        for (long x = 0 ; x < width ; x++) {
            //if (!im.pix(x,y) && im.pix(x,y-1)) {
            if (!im.pix(x,y) && im.pix(x,y-1) && (x == 0 || !im.pix(x-1,y)) && (x == width-1 || !im.pix(x+1,y))) {
                ys.push_back(y);
            }
        }
    }
    std::sort(ys.begin(), ys.end());
    return ys;
}

// The highest pixel in each column
std::vector<int> upper_envelope(PamImage<BWPixel> const& im) {
    long width  = im.getWidth();
    long height = im.getHeight();
	std::vector<int> envelope(width,-1);
    for (long x = 0; x < width; x++) {
        envelope[x] = height;
        for (long y = 0; y < height; y++) {
            if (im.pix(x,y)) {
                envelope[x] = y;
                break;
            }
        }
    }
    return envelope;
}
// The lowest pixel in each column
std::vector<int> lower_envelope(PamImage<BWPixel> const& im) {
    long width  = im.getWidth();
    long height = im.getHeight();
    std::vector<int> envelope(width,-1);
    for (long x = 0; x < width; x++) {
        envelope[x] = -1;
        for (long y = height-1; y >= 0 ; y--) {
            if (im.pix(x,y)) {
                envelope[x] = y;
                break;
            }
        }
    }
    return envelope;
}

// Draw an envelope image
PamImage<BWPixel> draw_envelope(std::vector<int> envelope, int height) {
    long width = (long) envelope.size();
    PamImage<BWPixel> im(width, height);
	for (long x = 0; x < width; x++) {
		if (envelope[x] >= 0 && envelope[x] < height) {
			im.pix(x,envelope[x]) = 1;
		}
	}
	return im;
}




// ----------------------------------------------------------------------------- : Boundaries

Coord Boundary::operator [] (int i) const {
	return i < 0  ?  points[(i +1)% size() + size()-1]//note: (-x) % x == 0
	              :  points[i % size()];
}

double Boundary::angle(int i, int j) const {
    Coord a = (*this)[i];
    Coord b = (*this)[j];
    double dx = a.x - b.x, dy = a.y - b.y;
    if (dx == 0 && dy == 0) return 0;
    return atan2(dy,dx);
}

// When tracing boundaries the current position will be marked with MARK
const BWPixel INK  = 1;
const BWPixel MARK = 2;

void Boundary::trace_boundary(int x, int y, int width, int height,PamImage<BWPixel> & im) {
	int x0 = x, y0 = y;
	int dx = -1, dy = 0;
	do {
		// check & set mark
		if (x < width && y < height) { 
			im.pix(x,y) |= MARK;
		}
		// add to boundary
		points.push_back(Coord(x,y));
		// which way to go?
		bool tl = y > 0      && x > 0     && (im.pix(x-1,y-1) & INK);
		bool tr = y > 0      && x < width && (im.pix(x  ,y-1) & INK);
		bool bl = y < height && x > 0     && (im.pix(x-1,y  ) & INK);
		bool br = y < height && x < width && (im.pix(x  ,y  ) & INK);
		int corner = (tl ? 0x1000 : 0)
		           | (tr ? 0x0100 : 0)
		           | (bl ? 0x0010 : 0)
		           | (br ? 0x0001 : 0);
		switch (corner) {
			case 0x0000: throw "Ended up inside background"; break;
			case 0x1111: throw "Ended up inside ink";        break;
			
			case 0x0001: dx =  0; dy = +1;                   break;
			case 0x0101: dx =  0; dy = +1;                   break;
			case 0x1101: dx =  0; dy = +1;                   break;
			
			case 0x0010: dx = -1; dy =  0;                   break;
			case 0x0011: dx = -1; dy =  0;                   break;
			case 0x0111: dx = -1; dy =  0;                   break;
			
			case 0x0100: dx = +1; dy =  0;                   break;
			case 0x1100: dx = +1; dy =  0;                   break;
			case 0x1110: dx = +1; dy =  0;                   break;
			
			case 0x1000: dx =  0; dy = -1;                   break;
			case 0x1010: dx =  0; dy = -1;                   break;
			case 0x1011: dx =  0; dy = -1;                   break;
			
			case 0x0110: case 0x1001: // turn left
				swap(dx,dy); dx = -dx, dy = -dy;
		}
		x += dx;
		y += dy;
	} while (x != x0 || y != y0);
}

void boundaries(PamImage<BWPixel> const & im, std::vector<Boundary>& out) {
    int width  = im.getWidth();
    int height = im.getHeight();
	
	
	// all starting points
	for (int y = 0 ; y < height ; ++y) {
		for (int x = 0 ; x < width ; ++x) {
			if (im.pix(x,y) == INK && (x == 0 || !(im.pix(x-1,y) & INK))) {
				out.push_back(Boundary());
				out.back().trace_boundary(x,y, width,height, const_cast<PamImage<BWPixel> &>( im) );
			}
		}
	}
	
	// clear marks
	for (int y = 0 ; y < height ; ++y) {
		for (int x = 0 ; x < width ; ++x) {
			const_cast<PamImage<BWPixel> &>(im).pix(x,y) &= 1;
		}
	}
}
