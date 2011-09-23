//+----------------------------------------------------------------------------+
//| Handwriting recognition - PamImage                                         |
//| Read and write binary .pbm, .pgm and .ppm files, manipulate images         |
//|                                                                            |
//| By Axel Brink and Tijn van der Zant                                        |
//+----------------------------------------------------------------------------+

#pragma warning(disable:4201)

#ifndef PAMIMAGE_H
#define PAMIMAGE_H
#include "../HwrConfig.h"
#include <boost/multi_array.hpp>
//#include <boost/shared_ptr.hpp>
#include <boost/shared_array.hpp>

// ----------------------------------------------------------------------------- : Includes

// ----------------------------------------------------------------------------- : Pixels

typedef unsigned char byte;
typedef unsigned int uint;

const uint AMask = 0xff000000u;
const int AShift = 24;
const uint RMask = 0x00ff0000u;
const int RShift = 16;
const uint GMask = 0x0000ff00u;
const int GShift = 8;
const uint BMask = 0x000000ffu;
const int BShift = 0;

struct RGBPixel {
	union {
		unsigned int Data;
		struct {
			byte b;//b,g,r,a  not a,r,g,b
			byte g;
			byte r;
			byte a;
		};
	};

	inline RGBPixel() {}
	inline RGBPixel(byte r, byte g, byte b) : r(r), g(g), b(b), a(0) {}
	inline RGBPixel(byte r, byte g, byte b,byte a) : r(r), g(g), b(b), a(a) {}
	inline RGBPixel(unsigned int data) : Data(data) {}
};


typedef signed char BWPixel; //HACK: to distinguish from GrayPixel

typedef unsigned char GrayPixel;

typedef long IntPixel;

/// Convert to a GrayPixel
inline GrayPixel to_gray(BWPixel   a) { return a == 0 ? 0 : 255; }
inline GrayPixel to_gray(GrayPixel a) { return a; }
inline GrayPixel to_gray(RGBPixel  a) { return (GrayPixel)((a.r + a.g + a.b) / 3); }
inline GrayPixel to_gray(IntPixel  a) { return (GrayPixel)a; } // the best we can do

/// Convert from a GrayPixel
inline void from_gray(BWPixel&   a, GrayPixel v) { a  =  v < 128 ? 0 : 1; }
inline void from_gray(GrayPixel& a, GrayPixel v) { a  =  v; }
inline void from_gray(RGBPixel&  a, GrayPixel v) { a.r = a.g = a.b = v; }
inline void from_gray(IntPixel&  a, GrayPixel v) { a  =  v; }

// ----------------------------------------------------------------------------- : PamImage


template<typename T>
class PamImage {
public:
	typedef boost::multi_array<T, 2> pixels;

//	int getWidth() const {return (int)storage->shape()[1];}
	//int getHeight() const {return (int)storage->shape()[0];}
	inline int getWidth() const {return width;}
	inline int getHeight() const {return height;}
	inline int getSize() const {return getWidth()*getHeight();}

	inline void tryPut(int x, int y, T val) {if ((x >= 0) && (x < getWidth()) && (y >= 0) && (y < getHeight())) pix(x,y)=val;}

	//T& pix(int x,int y) {return (*storage)[y][x];}
	//T pix(int x,int y) const {return (*storage)[y][x];}
	inline T& pix(int x,int y) {return storage[y*width+x];}
	inline T pix(int x,int y) const {return storage[y*width+x];}

	PamImage(int width, int height) 
		//: storage(new pixels(boost::extents[height][width]))
		: storage(new T[width*height]),
		width(width),
		height(height)
	{ 
		for (long y = 0 ; y < getHeight() ; ++y) {
			for (long x = 0 ; x < getWidth() ; ++x) {
				pix(x,y) = 0;
			}
		}
	}
	PamImage() : storage() {}

	// Convert to new image type
	template<typename TO> PamImage<TO> convert() const ;
	template<> PamImage<T> convert<T>() const {
		return PamImage<T>(*this);
	}
	//	PamImage<T> convert() const;
private:

	//boost::shared_ptr<pixels> storage;
	boost::shared_array<T> storage;
	int width; int height;
};

typedef PamImage<BWPixel>  ImageBW;
typedef PamImage<RGBPixel> ImageRGB;
typedef PamImage<GrayPixel> ImageGray;



template<> template <typename TO>
PamImage<TO> PamImage<IntPixel>::convert() const
{
	// determine range of values
	IntPixel minval = get_minvalInt32();
	IntPixel maxval = get_minvalInt32();
	int range = maxval - minval;
	if (range == 0) range = 1;
	float scale = 255.0f / (float) (range);
	// convert
	PamImage<TO> image(getWidth(),getHeight());
	for (long y = 0; y < getHeight(); ++y) {
		for (long x = 0; x < getWidth(); ++x) {
			from_gray(image.pix(x,y), (int)(scale * (pix(x,y) - minval)));
		}
	}
	return image;
}


template<typename T> template <typename TO>
PamImage<TO> PamImage<T>::convert()const {
	PamImage<TO> image(getWidth(), getHeight());
	for (long y = 0; y < getHeight(); ++y) {
		for (long x = 0; x < getWidth(); ++x) {
			from_gray(image.pix(x,y), to_gray(pix(x,y)));
		}
	}
	return image;
}


#endif
