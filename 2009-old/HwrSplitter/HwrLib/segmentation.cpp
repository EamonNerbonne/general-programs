//+----------------------------------------------------------------------------+
//| Handwriting recognition - Image tools                                      |
//| Image segmentation                                                         |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes

#include "stdafx.h"
#include "image/segmentation.h"

using std::min;
using std::max;

// ----------------------------------------------------------------------------- : Segmentation

inline bool operator ! (RGBPixel p) {
	return p.r == 0 && p.g == 0 && p.b == 0;
}
int dist(RGBPixel a, RGBPixel b) {
	return abs(a.r - b.r) + abs(a.g - b.g) + abs(a.b - b.b);
}
inline bool operator == (RGBPixel a, RGBPixel b) {
	return a.r == b.r && a.g == b.g && a.b == b.b;
}

ImageComponent::ImageComponent(RGBPixel label)
	: label(label)
	, min_x(INT_MAX), max_x(0)
	, min_y(INT_MAX), max_y(0)
	, min_xy(INT_MAX), max_xy(0)
	, mean_xy(0)
	, size(0)
{}

SegmentedImage::SegmentedImage(PamImage<BWPixel> const& im)
	: PamImage<RGBPixel>( im.getWidth(), im.getHeight())
{
	for (long y = 0 ; y < getHeight() ; ++y) {
		for (long x = 0 ; x < getWidth() ; ++x) {
			if (im.pix(x,y) && !pix(x,y)) {
				// found a new root, fill with a new color
				ImageComponent component(newColor());
				floodfill(x,y, im, component);
				component.mean_xy /= component.size;
				components.push_back(component);
			}
		}
	}
}

void SegmentedImage::floodfill(long xx, long yy, PamImage<BWPixel> const& im, ImageComponent& component) {
	struct P{
		long x,y;
		P(long x, long y): x(x),y(y){}
	};

	std::stack<P> points;
	points.push(P(xx,yy));
	while(!points.empty()) {
		P p = points.top();
		points.pop();
		long l = p.x, r = p.x; // l = lowest x to fill, r = after largest x to fill
		while (l - 1 >= 0 && im.pix(l-1,p.y) && !pix(l-1,p.y)) l--;
		while (r < getWidth()  && im.pix(r,p.y)   && !pix(r,p.y))   r++;
		for (long x2 = l ; x2 < r ; ++x2) {
			pix(x2,p.y) = component.label;
		}
		if (l < r) {
			component.min_x = min(component.min_x, l);
			component.max_x = max(component.max_x, r-1);
			component.min_y = min(component.min_y, p.y);
			component.max_y = max(component.max_y, p.y);
			component.min_xy = min(component.min_xy, p.x+p.y);
			component.max_xy = max(component.max_xy, p.x+p.y);
			component.mean_xy += (r-l)*(p.x+p.y);
			component.size += r-l;
		}
		for (long x2 = max(0L,l-1) ; x2 < min<int>(getWidth(),r+1) ; ++x2) {
			if (p.y - 1 >= 0     && im.pix(x2,p.y-1) && !pix(x2,p.y-1)) points.push(P(x2, p.y-1));
			if (p.y + 1 < getHeight() && im.pix(x2,p.y+1) && !pix(x2,p.y+1)) points.push(P(x2, p.y+1));
		}
	}
}

RGBPixel SegmentedImage::newColor() {
	again:
		RGBPixel color(rand()%255,rand()%255,rand()%255);
		// light enough
		if (color.r + color.b + color.g < 255) {
			int largest = max(color.r, max(color.g, color.b));
			if      (color.r == largest) color.r = 255 - (color.b + color.g);
			else if (color.g == largest) color.g = 255 - (color.r + color.b);
			else if (color.b == largest) color.b = 255 - (color.g + color.r);
		}
		// not used already
		int min_dist = components.size() < 3   ? 150
		             : components.size() < 6   ? 100
		             : components.size() < 20  ?  20
		             : components.size() < 100 ?  10
		             :                             1;
		for (size_t i = 0 ; i < components.size() ; ++i) {
			if (dist(components[i].label, color) < min_dist) goto again;
		}
		return color;
}

// ----------------------------------------------------------------------------- : Recolor

void SegmentedImage::recolor(ImageComponent const& c, RGBPixel b) {
	for (long y = 0 ; y < getHeight() ; ++y) {
		for (long x = 0 ; x < getWidth() ; ++x) {
			if (pix(x,y) == c.label) pix(x,y) = b;
		}
	}
}
