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

PamImage<double> featuresImage(ImageBW shearedImg, float shear) {
	ImageBW unsheared = unshear(shearedImg,shear);
	ImageFeatures feats(unsheared,-1,-1);
	PamImage<double> featsImg(feats.getImageWidth(),NUMBER_OF_FEATURES);
	for(int x=0;x<featsImg.getWidth();x++) {
		FeatureVector const & featsV = feats.featAt(x);
		for(int y=0;y<NUMBER_OF_FEATURES;y++) 
			featsImg.pix(x,y) =featsV[y];
	}

	return featsImg;
}

PamImage<BWPixel> processAndUnshear(PamImage<BWPixel> const& input_image, float shear_angle, int bodyTop, int bodyBot) {
    SegmentedImage seg_image(input_image );
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

double median(vector<double> values) {
	sort(values.begin(), values.end());
	return values[values.size()/2];
}
double mean(const vector<double>& values) {
	double sum = 0;
	for (size_t i = 0 ; i < values.size() ; ++i) sum += values[i];
	return sum / values.size();
}

void blur_projection(vector<double>& values, int radius) {
	vector<double> out(values.size());
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


void convolve(vector<double>& values, const vector<double>& window) {
	vector<double> out(values.size());
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
void add_gaussian(vector<double>& values, int mean, double sigma2s, double height) {
	for (size_t i = 0 ; i < values.size() ; ++i) {
		values[i] += exp(- sqr((double)i - (double)mean) / sigma2s) * height;
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


pair<vector<double>,vector<double> > weightedEdgePos(PamImage<BWPixel> vert_edge, vector<double> const & row_histo, vector<double> const & col_histo,  int edgeClass, vector<double> bodyTop, vector<double> bodyBot){
	long width     = vert_edge.getWidth();
	long height    = vert_edge.getHeight();
	pair<vector<double>,vector<double> > weightedEdgePosition= make_pair(vector<double>(width), vector<double>(width));
	int edge =  edgeClass>0?0:height-1;
	for(long x=0;x<width;++x) {
		double weightedpos=0.0, weight=0.0;
		if(edgeClass == 1) {
			double scale = 2.0 / bodyBot[x];
			for (long y = 0; y < height; ++y) {
				if(vert_edge.pix(x,y) == edgeClass) {
					double w = scale*( (y<bodyTop[x] ? y : y<bodyBot[x] ? (bodyBot[x] - y) : 0.0));
					w=row_histo[y]*w*w*w/sqrt(col_histo[x]);
					if(weightedpos==0.0) w*=2;
					if(x>1) w*= (abs(vert_edge.pix(x-2,y))+abs(vert_edge.pix(x-1,y))+1);
					weightedpos+=w*y;
					weight+=w;
				}
			}
		} else {
			double scale = 2.0/(edge-bodyTop[x]);
			for (long y = height-1; y >=0; --y) {
				if(vert_edge.pix(x,y) == edgeClass ) {
					double wA = 1.0-min(1.0,abs(y-bodyBot[x])/(bodyBot[x]-bodyTop[x]));
					double w = (y>bodyBot[x] ? 3*(edge-y)*scale : y>bodyTop[x] ? (y-bodyTop[x])*scale : 0.0);
					w=row_histo[y]*wA*w*w/sqrt(col_histo[x]);
					if(weightedpos==0.0) w*=3;
					if(x>1) w*= (abs(vert_edge.pix(x-2,y))+abs(vert_edge.pix(x-1,y))+1);
					weightedpos+=w*(y+1);
					weight+=w;
				}
			}
		}

		weightedEdgePosition.first[x]=weightedpos;
		weightedEdgePosition.second[x]=weight;
	}
	return weightedEdgePosition;
}

vector<double> leftTriangularWeightedSum(vector<double> f) {
	//for each n, computes sum of f(i) over i<-0..n weighted by (i+1/n+1)
	for(int i=1;i<f.size();++i) 
		f[i]+=double(i)/double(i+1) * f[i-1];
	return f;
}

vector<double> rightExTriWeightedSum(vector<double> f) {
	//symmetric to left but excluding current value.
	vector<double> r(f.size());

	for(int i=(int)f.size()-2;i>=0;--i) 
		r[i] = double(f.size()-i-1)/double(f.size()-i) * (r[i+1] + f[i+1]);
	return r;
}

vector<double> add(vector<double> a, vector<double> b) {
	for(int i=0;i<a.size();++i)
		a[i]+=b[i];
	return a;
}
vector<double> add(vector<double> a, double b) {
	for(int i=0;i<a.size();++i)
		a[i]+=b;
	return a;
}

vector<double> scale(vector<double> a, double x) {
	for(int i=0;i<a.size();++i) a[i]*=x;
	return a;
}

vector<double> TriSum(vector<double> vals) {
	return add(leftTriangularWeightedSum(vals), rightExTriWeightedSum(vals));
}

vector<double> ApplyWeights(vector<double> weightedvals, vector<double> weights) {
	for(int i=0; i < weightedvals.size(); ++i)
		weightedvals[i] /= weights[i];
	return weightedvals;
}

vector<double> TriSmooth(vector<double> weightedvals, vector<double> weights) {	return ApplyWeights(TriSum(weightedvals),TriSum(weights)); }

vector<double> sqBlur(vector<double> x) {
	fastblur(x,(int)x.size(),35,4,0.7);
	return x;
}



pair<vector<double>, vector<double> > smoothBody(PamImage<BWPixel> const& vert_edge, vector<double> const & row_histo, vector<double> const & col_histo, vector<double> bodyTop, vector<double> bodyBot) {
	//vert_edge: 1 on top edge, -1 on bottom edge, 0 elsewhere.
	//now we want to compute the localized top of the body.  This is just the "normal" mean y-coordinate of the top edge.
	//so, we compute the mean weighted edge position per column...
	vector<double> smoothOldTop =TriSmooth(bodyTop,vector<double>(bodyTop.size(), 1.0));
	vector<double> smoothOldBot = TriSmooth(bodyBot,vector<double>(bodyBot.size(), 1.0));


	auto topEdges = weightedEdgePos(vert_edge,row_histo,col_histo, 1, smoothOldTop, smoothOldBot);
	auto botEdges = weightedEdgePos(vert_edge, row_histo,col_histo,-1, smoothOldTop, smoothOldBot);

	vector<double> smoothTop = TriSmooth(topEdges.first, topEdges.second);
	vector<double> smoothBot = TriSmooth(botEdges.first, botEdges.second);

	vector<double> blurTop = ApplyWeights(add(sqBlur(topEdges.first), scale(bodyTop,0.05)), add(sqBlur(topEdges.second),0.05));
	vector<double> blurBot = ApplyWeights(add(sqBlur(botEdges.first), scale(bodyBot,0.05)), add(sqBlur(botEdges.second),0.05));
	
	vector<double> top = add(scale(smoothTop,0.5), scale(blurTop,0.5));
	vector<double> bot = add(scale(smoothBot,0.5), scale(blurBot,0.5));

	return make_pair(top,bot);
}


PamImage<BWPixel> fixBody(PamImage<BWPixel> const& im_in, int bodyTop, int bodyBot) {
	long width     = im_in.getWidth();
	long height    = im_in.getHeight();
	PamImage<BWPixel> vert_edge(width, height);
	for(long x=0;x<width;x++) 
		vert_edge.pix(x,0) = im_in.pix(x,0);

	for (long y = 1; y < height; ++y) 
		for(long x=0;x<width;++x) 
			vert_edge.pix(x,y) = im_in.pix(x,y)-im_in.pix(x,y-1);

	vector<double> row_histo(height, 0.0);
	vector<double> col_histo(width, 0.0);

	project_x(im_in,row_histo,0,width);
	project_y(im_in,col_histo,0,height);
	fastblur(row_histo,row_histo.size(),5,3,0.9);
	fastblur(col_histo,col_histo.size(),3,3,0.9);
	

	vector<double> top(width,double(bodyTop));
	vector<double> bot(width,double(bodyBot));
	auto body = make_pair(vector<double>(width,double(bodyTop)), vector<double> (width,double(bodyBot)));

	for(int i=0;i<3;++i){
		body = smoothBody(vert_edge,row_histo,col_histo,body.first,body.second);
	}

	for (long y = 0; y < height; ++y) 
		for(long x=0;x<width;++x) 
			if(vert_edge.pix(x,y) <0) 
				vert_edge.pix(x,y) = 1;


	for(long x=0;x<width;++x) {
		vert_edge.pix(x,(int)(body.first[x]+0.5)) = 1;
		vert_edge.pix(x,(int)(body.second[x]+0.5)) = 1;
	}


	return vert_edge;
}

