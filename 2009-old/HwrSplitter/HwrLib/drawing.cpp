//+----------------------------------------------------------------------------+
//| Handwriting recognition - Image tools                                      |
//| Drawing lines & rectangles                                                 |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include "image/drawing.h"

using std::swap;
using std::min;
using std::max;

// ----------------------------------------------------------------------------- : Drawing


void line_gray(PamImage<GrayPixel> &im, int x1, int y1, int x2, int y2, GrayPixel grayval)
{
	int width = im.getWidth();
	int height = im.getHeight();

	// adapted from http://en.wikipedia.org/wiki/Bresenham's_line_algorithm 2007-07-18

	bool steep = (abs(y2 - y1) > abs(x2 - x1));
	if (steep)
	{
		swap(x1, y1);
		swap(x2, y2);
	}
	if (x1 > x2)
	{
		swap(x1, x2);
		swap(y1, y2);
	}
	int deltax = x2 - x1;
	int deltay = abs(y2 - y1);
	int error = -deltax / 2;
	int ystep;
	int y = y1;
	if (y1 < y2)
	{
		ystep = 1;
	}
	else
	{
		ystep = -1;
	}
	for (int x = x1; x <= x2; ++x)
	{
		if (steep)
		{
			im.tryPut(y,x,grayval);
		}
		else
		{
			im.tryPut(x,y,grayval);
		}
		error += deltay;
		if (error > 0)
		{
			y += ystep;
			error -= deltax;
		}	
	}
}

void block_gray(PamImage<GrayPixel> &im, int x1, int y1, int x2, int y2, GrayPixel grayval) {
	int width = im.getWidth();
	int height = im.getHeight();

	x1 = max(0, min(width  - 1, x1));
	x2 = max(0, min(width  - 1, x2));
	y1 = max(0, min(height - 1, y1));
	y2 = max(0, min(height - 1, y2));

	for (int y = y1; y <= y2; ++y) {
		for (int x = x1; x <= x2; ++x) {
			from_gray(im.pix(x,y), grayval);
		}
	}
}

void block_rgb(PamImage<RGBPixel> &im, int x1, int y1, int x2, int y2, RGBPixel color, float alpha) {
	int width = im.getWidth();
	int height = im.getHeight();

	x1 = max(0, min(width  - 1, x1));
	x2 = max(0, min(width  - 1, x2));
	y1 = max(0, min(height - 1, y1));
	y2 = max(0, min(height - 1, y2));

	
	for (int y = y1; y <= y2; ++y) {
		for (int x = x1; x <= x2; ++x) {
			im.pix(x,y).r = (byte)(im.pix(x,y).r + alpha*(color.r - im.pix(x,y).r));
			im.pix(x,y).g = (byte)(im.pix(x,y).g + alpha*(color.g - im.pix(x,y).g));
			im.pix(x,y).b = (byte)(im.pix(x,y).b + alpha*(color.b - im.pix(x,y).b));
		}
	}
}
void horizontal_line_rgb(PamImage<RGBPixel>& im, int y, RGBPixel color, float alpha) {
	block_rgb(im, -1, y, INT_MAX, y, color, alpha);
}

void draw_x_projection(PamImage<RGBPixel>& im, int x0, int x1, const std::vector<Float>& histo, RGBPixel color, float alpha) {
	if (histo.empty()) return;

	int width = im.getWidth();
	int height = im.getHeight();
	height = min(height, (int)histo.size());

	x0 = max(0, min(width-1, x0));
	x1 = max(0, min(width-1, x1));

	double largest = *std::max_element(histo.begin(), histo.end());

	for (int y = 0; y < height; ++y) {
		double fraction = histo[y] / largest;
		int x1b = x0 + (int)((x1-x0) * fraction);
		for (int x = x0; x <= x1b; ++x) {
			im.pix(x,y).r = (byte)(im.pix(x,y).r + alpha*(color.r - im.pix(x,y).r));
			im.pix(x,y).g = (byte)(im.pix(x,y).g + alpha*(color.g - im.pix(x,y).g));
			im.pix(x,y).b = (byte)(im.pix(x,y).b + alpha*(color.b - im.pix(x,y).b));
		}
	}
}

void draw_y_projection(PamImage<RGBPixel>& im, int y0, int y1, const std::vector<Float>& histo, RGBPixel color, float alpha) {
	if (histo.empty()) return;

	int width  = im.getWidth();
	int height = im.getHeight();
	width = min(width, (int)histo.size());

	y0 = max(0, min(height-1, y0));
	y1 = max(0, min(height-1, y1));

	double largest = *std::max_element(histo.begin(), histo.end());
	for (int x = 0; x < width; ++x) {
		double fraction = histo[x] / largest;
		int y1b = y0 + (int)((y1-y0) * fraction);
		for (int y = min(y0,y1b); y <= max(y0,y1b); ++y) {
			im.pix(x,y).r = (byte)(im.pix(x,y).r + alpha*(color.r - im.pix(x,y).r));
			im.pix(x,y).g = (byte)(im.pix(x,y).g + alpha*(color.g - im.pix(x,y).g));
			im.pix(x,y).b = (byte)(im.pix(x,y).b + alpha*(color.b - im.pix(x,y).b));
		}
	}
}



int hsl2rgbp(double t1, double t2, double t3) {
	// adjust t3 to [0...1)
	if      (t3 < 0.0) t3 += 1;
	else if (t3 > 1.0) t3 -= 1;
	// determine color
	if (6.0 * t3 < 1) return (int)(255 * (t1 + (t2-t1) * 6.0 * t3)             );
	if (2.0 * t3 < 1) return (int)(255 * (t2)                                  );
	if (3.0 * t3 < 2) return (int)(255 * (t1 + (t2-t1) * 6.0 * (2.0/3.0 - t3)) );
	else              return (int)(255 * (t1)                                  );
}
RGBPixel hsl2rgb(double h, double s, double l) {
	double t2 = l < 0.5 ? l * (1.0 + s) :
		l * (1.0 - s) + s;
	double t1 = 2.0 * l - t2;
	return RGBPixel(
		hsl2rgbp(t1, t2, h + 1.0/3.0),
		hsl2rgbp(t1, t2, h)          ,
		hsl2rgbp(t1, t2, h - 1.0/3.0)
		);
}

void draw_boundary(PamImage<RGBPixel>& im, const Boundary& b) {
	int width  = im.getWidth();
	int height = im.getHeight();

	for (int i = 0 ; i < b.size() ; ++i) {
		Coord p = b[i];
		RGBPixel color = hsl2rgb(b.angle(i - 3, i + 2) / M_PI / 2 + 0.5, 1, 0.5);
		//RGBPixel color = hsl2rgb(fmod((atan2(dy1,dx1) - atan2(dy2,dx2)) / M_PI / 2 + 0.5, 1), 1, 0.5);
		if (p.x < width && p.y < height) {
			im.pix(p.x,p.y) = color;
		}
	}
}
