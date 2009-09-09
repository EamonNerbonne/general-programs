//+----------------------------------------------------------------------------+
//| Handwriting recognition - Preprocessing                                    |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include <boost/timer.hpp>
#include <boost/progress.hpp>
#include "image/preprocess.h"
#include "util/for_each.hpp"
#include "image/segmentation.h"
#include "image/histograms.h"
#include "image/transformations.h"
#include "feature/features.h"

using namespace std;


DECLARE_TYPEOF_COLLECTION(ImageComponent);

// ----------------------------------------------------------------------------- : Preprocessing pipeline

bool removeComponent(const ImageComponent& c, int width, int height,int bodyTop,int bodyBot) {
    if( c.size < 15) {
        return true;
    } else if (c.max_y == height - 1 && c.min_y >= bodyBot) {
        return true;
    } else if (c.min_y >= bodyBot && c.size < 200) { //segments under the body can be discarded - these aren't dots.
        return true;
    } else if (c.min_y == 0 && c.max_y < bodyTop) {
        return true;
    } else if (c.min_y == 0 && c.max_x == width - 1 && c.min_x > width - 100) {//bad for whole lines?
        return true;
    } else if (c.min_y == 0 && c.min_x == 0 && c.max_x < 100) {
        return true;
    } else if (c.mean_xy < height) { //huh?
        return true;
    } else if (c.mean_xy > width) { //huh?
        return true;
    } else if (c.max_xy - c.min_xy <= 30 && c.min_y == 0 && c.max_y < height * 0.55) { //huh?
        return true;
    } else {
        return false;
    }
}

PamImage<BWPixel> preprocess(PamImage<RGBPixel> const& input_image, float shear_angle,int bodyTop,int bodyBot) {
    PamImage<GrayPixel> grey_image(red_channel(input_image));
    PamImage<BWPixel> bw_image (threshold(grey_image, 200)); // some ink is almost white
    SegmentedImage seg_image( bw_image );
    #ifdef TRACE_PREPROCESSING_STEPS // DEBUG OUTPUT
		grey_image.save("word-grey.pgm");
		bw_image  .save("word-bw.pbm");
		seg_image .save("word-seg.ppm");
    #endif
    // what segments to keep?
    long width = input_image.getWidth(), height = input_image.getHeight();
    FOR_EACH(c, seg_image.components) {
        // remove this component?
        if (removeComponent(c, width, height,bodyTop,bodyBot)) {
            seg_image.recolor(c, RGBPixel(50,50,50));
        } else {
            seg_image.recolor(c, RGBPixel(c.label.r/4+191,c.label.g/4+191,c.label.b/4+191));
        }
    }
    PamImage<BWPixel> bw_seg_image = seg_image.convert<BWPixel>();
    PamImage<BWPixel> unseared_image = unshear(bw_seg_image, shear_angle);
    #ifdef TRACE_PREPROCESSING_STEPS // DEBUG OUTPUT
		seg_image.save("word-seg2.ppm");
		bw_seg_image.save("word-seg-bw.pbm");
		unseared_image.save("word-unsheared.pbm");
    #endif
    return crop_to_ink(unseared_image);
}


PamImage<BWPixel> preprocessLimited(PamImage<RGBPixel> const& input_image) {
	using namespace boost;
	PamImage<GrayPixel> grey_image;
	{progress_timer t;
	grey_image = red_channel(input_image);}
	printf("\np%x\n",(unsigned)input_image.pix(1000,2000).Data );
	printf("\np%x\n",(unsigned)grey_image.pix(1000,2000));
	{progress_timer t;
    PamImage<BWPixel> bw_image   = threshold(grey_image, 200); // some ink is almost white
	return bw_image;
	}
}

PamImage<Float> featuresImage(ImageBW shearedImg, float shear) {
	ImageBW unsheared = unshear(shearedImg,shear);
	ImageFeatures feats(unsheared,-1,-1);
	PamImage<Float> featsImg(feats.getImageWidth(),NUMBER_OF_FEATURES);
	for(int x=0;x<featsImg.getWidth();x++) {
		FeatureVector const & featsV = feats.featAt(x);
		for(int y=0;y<NUMBER_OF_FEATURES;y++) 
			featsImg.pix(x,y) =featsV[y];
	}

	return featsImg;
}

PamImage<BWPixel> processAndUnshear(PamImage<BWPixel> const& input_image, float shear_angle, int bodyTop, int bodyBot) {
    SegmentedImage seg_image( input_image );
    // what segments to keep?
    long width = input_image.getWidth(), height = input_image.getHeight();
    FOR_EACH(c, seg_image.components) {
        // remove this component?
        if (removeComponent(c, width, height,bodyTop,bodyBot)) {
            seg_image.recolor(c, RGBPixel(50,50,50));
        } else {
            seg_image.recolor(c, RGBPixel(c.label.r/4+191,c.label.g/4+191,c.label.b/4+191));
        }
    }
    PamImage<BWPixel> bw_seg_image = seg_image.convert<BWPixel>();
    return unshear(bw_seg_image, shear_angle);
}



// ----------------------------------------------------------------------------- : Utility

Float median(vector<Float> values) {
	sort(values.begin(), values.end());
	return values[values.size()/2];
}
Float mean(const vector<Float>& values) {
	Float sum = 0;
	for (size_t i = 0 ; i < values.size() ; ++i) sum += values[i];
	return sum / values.size();
}

void blur_projection(vector<Float>& values, int radius) {
	vector<Float> out(values.size());
	for (int i = 0 ; i < (int)values.size() ; ++i) {
		int n = 0;
		for (int j = max(0, i-radius) ; j < min(i+radius+1, (int)values.size()) ; ++j) {
			out[i] += values[j];
			n++;
		}
		out[i] /= n;
	}
	swap(out, values);
}


void convolve(vector<Float>& values, const vector<Float>& window) {
	vector<Float> out(values.size());
	int winsize = (int)window.size();
	int valsize = (int)values.size();
	int offset=(winsize/2);
	for (int i = 0 ; i < (int)values.size() ; ++i) {
		for (int j = 0; j < (int)window.size() ; ++j) {
            int pos = valsize -1- abs(abs(i+j - offset)-(valsize-1));
			out[i] += values[pos]*window[j];
		}
	}
	swap(out, values);
}

// Add a (scaled) gaussian to a vector
//   v[x] := v[x] + N(mean,sigma)(x)*scale
//   where N has a max value of 1
//
//   sigma2s = 2 * sigma^2
void add_gaussian(vector<Float>& values, int mean, Float sigma2s, Float height) {
	for (size_t i = 0 ; i < values.size() ; ++i) {
		values[i] += exp(- sqr((Float)i - (Float)mean) / sigma2s) * height;
	}
}



// ----------------------------------------------------------------------------- : Pixel type conversion
PamImage<GrayPixel> red_channel(PamImage<RGBPixel> const im_in) {
	int width  = im_in.getWidth();
	int height = im_in.getHeight();
	PamImage<GrayPixel> im_out(width, height);
//	printf("pD: %x\n",(unsigned)im_in.pix(1000,73).Data );

	//printf("\np%u\n",(unsigned)grey_image.pix(1000,2000));

	// copy red channel
	for (int y = 0; y < height; y++) {
		for (int x = 0; x < width; x++) {
			im_out.pix(x,y) = im_in.pix(x,y).r;
		}
	}
	return im_out;
}

PamImage<BWPixel> threshold(PamImage<GrayPixel> const im_in, int threshold) {
	int width  = im_in.getWidth();
	int height = im_in.getHeight();
	PamImage<BWPixel> im_out(width, height);
	// threshold
	for (int y = 0; y < height; y++) {
		for (int x = 0; x < width; x++) {
			im_out.pix(x,y) = im_in.pix(x,y) >= threshold ? 0 : 1;
		}
	}
	return im_out;
}

PamImage<BWPixel> invert(PamImage<BWPixel> const im_in) {
	int width  = im_in.getWidth();
	int height = im_in.getHeight();
	PamImage<BWPixel> im_out(width, height);
	// invert
	for (int y = 0; y < height; y++) {
		for (int x = 0; x < width; x++) {
			im_out.pix(x,y) = 1 - im_in.pix(x,y);
		}
	}
	return im_out;
}

