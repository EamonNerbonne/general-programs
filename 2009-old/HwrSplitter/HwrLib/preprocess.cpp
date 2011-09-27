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
#include "EdgeFollow.h"

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


template <class TDir, class TWhile, class TAction> EdgeComponent BodyEdgeFollow(PamImage<BWPixel> const & image, EdgeComponent initial, TDir step,int & minX, int& minY, int& maxX, int& maxY,
	TWhile filter,   
	TAction action) {
	action(initial.P);
	EdgeComponent pos(step(image,initial));
	while(filter(pos) && pos != initial) {
		action(pos.P);
		minY = min(minY, pos.P.Y);
		maxY = max(maxY, pos.P.Y);
		minX = min(minX,pos.P.X);
		maxX = max(maxX,pos.P.X);
		pos=step(image,pos);
	}
	return pos;
}

pair<vector<double>,vector<double> > weightedTopEdgePos(PamImage<BWPixel> const & image,  PamImage<unsigned> const &sum, PamImage<BWPixel> const & vert_edge, 
	 vector<double>const &  bodyTop, vector<double>const &  bodyBot){
	int width     = vert_edge.getWidth();
	int height    = vert_edge.getHeight();
	pair<vector<double>,vector<double> > weightedEdgePosition= make_pair(vector<double>(width), vector<double>(width));
	vector<int> componentTop(width,height);
	for(int x=1;x<width-1;++x) {
		double weightedpos=0.0, weight=0.0;
		double bodyheight = max(0.0,bodyBot[x]-bodyTop[x]);
		for (int y = 0; y < height; ++y) {
			if(vert_edge.pix(x,y) == 1) {
				EdgeComponent cur = EdgeComponent(Point(x,y),TopEdge);
				EdgeComponent walkFwd = ClockwiseSteps(image,cur,16);
				EdgeComponent walkBack = CounterClockwiseSteps(image,cur,16);

				double wA;
				if(abs(walkFwd.P.X-walkBack.P.X) < abs(walkFwd.P.Y - walkBack.P.Y)
					||abs(cur.P.X-walkBack.P.X) < abs(cur.P.Y - walkBack.P.Y)
					||abs(walkFwd.P.X-cur.P.X) < abs(walkFwd.P.Y - cur.P.Y)
					||abs(walkFwd.P.Y-cur.P.Y)<2||abs(cur.P.Y-walkBack.P.Y)<2
					//||cur.P.Y>walkBack.P.Y ||cur.P.Y>walkFwd.P.Y
					) {
					wA=0.0;
				} else if(y<bodyTop[x]) {//outside current body estimate:

					int minY=y, maxY=y;
					int minX=x, maxX=x;
					EdgeComponent clockwise = 
						BodyEdgeFollow(image,cur,ClockwiseStep,minX,minY,maxX,maxY,
							[&bodyTop](EdgeComponent e) {return e.P.Y < bodyTop[e.P.X];},
							[&componentTop](Point point) {componentTop[point.X] = min(componentTop[point.X],point.Y); }
							);
					EdgeComponent counterclockwise =
						BodyEdgeFollow(image,cur,CounterClockwiseStep,minX,minY,maxX,maxY,
							[&bodyTop](EdgeComponent e) {return e.P.Y < bodyTop[e.P.X];},
							[&componentTop](Point point) {componentTop[point.X] = min(componentTop[point.X],point.Y); }
						);
					
					int stemwidth = abs(clockwise.P.X - counterclockwise.P.X);
					double ascenderheight = maxY - minY;

					int emptyPx=0, filledPx=0;
					for(int cx=minX;cx<=maxX;++cx) {
						for(int cy=componentTop[cx];cy<bodyTop[cx];++cy)
							(image.pix(cx,cy) ? filledPx : emptyPx)++;
						componentTop[cx]=height;
					}

					if(clockwise.P.X > counterclockwise.P.X && stemwidth*2 > maxX-minX && stemwidth > ascenderheight && filledPx > emptyPx){
						wA = 5.0;
					}else
						wA = 0.0;
				} else
					wA = 1.0;



				double wB =  (y<bodyTop[x] ? y/bodyBot[x] : y<bodyBot[x] ? 1.0 : 0.0);
				double w=wA*wB;

				int bbot_y=min(height, (int)(y+bodyheight+0.5));
				int btop_y=(int)(bodyTop[x]+0.5);
				int rgt_x=min(width,  (int)(x+bodyheight+0.5));
				int lft_x=max(0,  (int)(x-bodyheight+0.5));
				int bodyArea = (rgt_x-lft_x)*(bbot_y-btop_y);
				int newArea = (rgt_x-lft_x)*(bbot_y-y);
				int bodySum = SumInRect(sum,lft_x,rgt_x,btop_y,bbot_y); 
				int newSum = SumInRect(sum,lft_x,rgt_x,y,bbot_y);
				if(bodyArea>0&&newArea>0)
					w *= max(0.0, 1.0 + 3*(newSum/double(newArea) - bodySum/double(bodyArea)));
				if(weightedpos==0.0) w*=3;
				//if(x>1) w*= (abs(vert_edge.pix(x-2,y))+abs(vert_edge.pix(x-1,y))+1);
				weightedpos+=w*max(y-3, 0);
				weight+=w;
			}
		}

		weightedEdgePosition.first[x]=weightedpos;
		weightedEdgePosition.second[x]=weight;
	}
	return weightedEdgePosition;
}

pair<vector<double>,vector<double> > weightedBotEdgePos( PamImage<BWPixel> const & image,  PamImage<unsigned> const &sum, PamImage<BWPixel>const & vert_edge,
	 vector<double> const & bodyTop, vector<double> const & bodyBot){
	int width     = vert_edge.getWidth();
	int height    = vert_edge.getHeight();
	pair<vector<double>,vector<double> > weightedEdgePosition= make_pair(vector<double>(width), vector<double>(width));
	vector<int> componentBottom(width,0);
	for(int x=1;x<width-1;++x) {
		double weightedpos=0.0, weight=0.0;
		//double scale = 2.0/(height-1-bodyTop[x]);
		double bodyheight = max(0.0,bodyBot[x]-bodyTop[x]);
		for (int y = height-1; y >=0; --y) {
			if( vert_edge.pix(x,y) == -1) {
				EdgeComponent cur = EdgeComponent(Point(x,y-1),BottomEdge);
				EdgeComponent walkFwd = ClockwiseSteps(image,cur,16);
				EdgeComponent walkBack = CounterClockwiseSteps(image,cur,16);

				double wA;

				if(
					abs(walkFwd.P.X-walkBack.P.X) < abs(walkFwd.P.Y - walkBack.P.Y)
					||abs(cur.P.X-walkBack.P.X) < abs(cur.P.Y - walkBack.P.Y)
					||abs(walkFwd.P.X-cur.P.X) < abs(walkFwd.P.Y - cur.P.Y)
					||abs(walkFwd.P.Y-cur.P.Y)<2||abs(cur.P.Y-walkBack.P.Y)<2
					//||cur.P.Y<walkBack.P.Y ||cur.P.Y<walkFwd.P.Y

					//abs(walkFwd.P.X-walkBack.P.X) < abs(walkFwd.P.Y - walkBack.P.Y)
					//||abs(cur.P.X-walkBack.P.X) < abs(cur.P.Y - walkBack.P.Y)
					//||abs(walkFwd.P.X-cur.P.X) < abs(walkFwd.P.Y - cur.P.Y)
					//||abs(walkFwd.P.Y-cur.P.Y)<2||abs(cur.P.Y-walkBack.P.Y)<2
					) {
					wA=0.0;
				} if(y>=bodyBot[x]) {//outside current body estimate:
					int minY=cur.P.Y,maxY=cur.P.Y;
					int minX=x, maxX=x;
					EdgeComponent clockwise = 
						BodyEdgeFollow(image,cur,ClockwiseStep,minX,minY,maxX,maxY,
							[&bodyBot](EdgeComponent e) {return e.P.Y >= bodyBot[e.P.X];},
							[&componentBottom](Point point) {componentBottom[point.X] = max(componentBottom[point.X],point.Y); }
							);
					EdgeComponent counterclockwise =
						BodyEdgeFollow(image,cur,CounterClockwiseStep,minX,minY,maxX,maxY,
							[&bodyBot](EdgeComponent e) {return e.P.Y >= bodyBot[e.P.X];},
							[&componentBottom](Point point) {componentBottom[point.X] = max(componentBottom[point.X],point.Y); }
						);
					
					int stemwidth = abs(clockwise.P.X - counterclockwise.P.X);
					double descenderdepth = maxY - minY;

					int emptyPx=0, filledPx=0;
					for(int cx=minX;cx<=maxX;++cx) {
						for(int cy=componentBottom[cx];cy>=bodyBot[cx];--cy)
							(image.pix(cx,cy) ? filledPx : emptyPx)++;
						componentBottom[cx]=0;
					}

					if(clockwise.P.X < counterclockwise.P.X && stemwidth*2 > maxX-minX && stemwidth > descenderdepth && filledPx > emptyPx){
						wA = 10.0;
					}else
						wA = 0.0;
				} else
					wA = 1.0;

				//double wB = scale*( (y>=bodyBot[x] ? height-y : y>=bodyTop[x] ? (y - bodyTop[x]) : 0.0));
				
				double wB =  (y>=bodyBot[x] ? y/(height-bodyTop[x]) : y>=bodyTop[x] ? 1.0 : 0.0);

				//(y>bodyBot[x] ? 3*(height-1-y)*scale : y>bodyTop[x] ? (y-bodyTop[x])*scale : 0.0);
				double w=wA*wB;
				if(weightedpos==0.0) w*=3;
				
				int bbot_y=min(height, (int)(y+bodyheight+0.5));
				int btop_y=(int)(bodyTop[x]+0.5);
				int rgt_x=min(width,  (int)(x+bodyheight+0.5));
				int lft_x=max(0,  (int)(x-bodyheight+0.5));
				int bodyArea = (rgt_x-lft_x)*(bbot_y-btop_y);
				int newArea = (rgt_x-lft_x)*(y-btop_y);
				int bodySum = SumInRect(sum,lft_x,rgt_x,btop_y,bbot_y); 
				int newSum = SumInRect(sum,lft_x,rgt_x,btop_y,y);
				if(bodyArea>0&&newArea>0)
					w *= max(0.0, 1.0 + 3*(newSum/double(newArea) - bodySum/double(bodyArea)));

				weightedpos+=w*min(y+3,height);
				weight+=w;
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
		weightedvals[i] = weightedvals[i]==0.0?0.0:weightedvals[i]/ weights[i];
	return weightedvals;
}

vector<double> TriSmooth(vector<double> weightedvals, vector<double> weights) {	return ApplyWeights(TriSum(weightedvals),TriSum(weights)); }

vector<double> sqBlur(vector<double> x) {
	fastblur(x,(int)x.size(),35,4,1.0);
	return x;
}



pair<vector<double>, vector<double> > smoothBody(PamImage<BWPixel> const & image, PamImage<unsigned> const &sum, PamImage<BWPixel> const& vert_edge, vector<double> bodyTop, vector<double> bodyBot, double smoothing, double momentum) {
	//vert_edge: 1 on top edge, -1 on bottom edge, 0 elsewhere.
	//now we want to compute the localized top of the body.  This is just the "normal" mean y-coordinate of the top edge.
	//so, we compute the mean weighted edge position per column...

	auto topEdges = weightedTopEdgePos(image,sum, vert_edge, bodyTop, bodyBot);
	auto botEdges = weightedBotEdgePos(image, sum, vert_edge, bodyTop, bodyBot);

	vector<double> smoothTop = TriSmooth(topEdges.first, topEdges.second);
	vector<double> smoothBot = TriSmooth(botEdges.first, botEdges.second);

	vector<double> blurTop = ApplyWeights(sqBlur(topEdges.first),sqBlur(topEdges.second));
	vector<double> blurBot = ApplyWeights(sqBlur(botEdges.first), sqBlur(botEdges.second));
	
	vector<double> top = add(add(scale(smoothTop, smoothing*(1-momentum)), scale(blurTop, (1-smoothing)*(1-momentum))), scale(bodyTop,momentum));
	vector<double> bot = add(add(scale(smoothBot , smoothing*(1-momentum)), scale(blurBot , (1-smoothing)*(1-momentum))), scale(bodyBot,momentum));

	return make_pair(top,bot);
}


PamImage<BWPixel> fixBody(PamImage<BWPixel> const& im_in, int bodyTop, int bodyBot) {
	int width     = im_in.getWidth();
	int height    = im_in.getHeight();
	PamImage<BWPixel> vert_edge(width, height);

	for(int x=0;x<width;x++) 
		vert_edge.pix(x,0) = im_in.pix(x,0);

	for (int y = 1; y < height; ++y) 
		for(int x=0;x<width;++x) 
			vert_edge.pix(x,y) = im_in.pix(x,y)-im_in.pix(x,y-1);
	
	PamImage<unsigned> sum = CumulativeCount(im_in);
	

	vector<double> top(width,double(bodyTop));
	vector<double> bot(width,double(bodyBot));
	auto body = make_pair(vector<double>(width,double(bodyTop)), vector<double> (width,double(bodyBot)));
	body = smoothBody(im_in,sum,vert_edge,body.first,body.second,0.8,0.2);
	body = smoothBody(im_in,sum,vert_edge,body.first,body.second,0.6,0.4);
	body = smoothBody(im_in,sum,vert_edge,body.first,body.second,0.4,0.6);
	body = smoothBody(im_in,sum,vert_edge,body.first,body.second,0.2,0.8);

	for (int y = 0; y < height; ++y) 
		for(int x=0;x<width;++x) 
			vert_edge.pix(x,y) = 0;
	
	/*
	for (long y = 0; y < height; ++y) 
		for(long x=0;x<width;++x) 
			if(vert_edge.pix(x,y) <0) 
				vert_edge.pix(x,y) = 1;
				*/
	set<EdgeComponent> knownEdges;

	for(int x=0;x<width;++x) {
		int topY = max(0, (int)(body.first[x]+0.5));
		int botY = min(height-1, (int)(body.second[x]+0.5));
		Point topP = Point(x,topY);
		Point botP = Point(x,botY);
		EdgeComponent consider[] = {
			EdgeComponent(topP,LeftEdge),EdgeComponent(topP,RightEdge),EdgeComponent(topP,BottomEdge),EdgeComponent(topP,TopEdge),
			EdgeComponent(botP,LeftEdge),EdgeComponent(botP,RightEdge),EdgeComponent(botP,BottomEdge),EdgeComponent(botP,TopEdge)
		};
		bool hasTop = topP.isSet(im_in), hasBot=botP.isSet(im_in);

		if(hasTop||hasBot)
			for(int i=0;i<8;i++) {
				bool isvalid = consider[i].IsValidIn(im_in);
				if(isvalid && knownEdges.find(consider[i]) == knownEdges.end()) {
					auto newEdge = ClockwiseEdge(im_in,consider[i]);
					for_each(newEdge.cbegin(),newEdge.cend(),[&knownEdges,&vert_edge](EdgeComponent e) {
						knownEdges.insert(e);
						vert_edge.pix(e.P.X,e.P.Y) = 1;
					});
				}
			}

		vert_edge.pix(x,topY) = 1;
		vert_edge.pix(x,botY) = 1;
		for(int y=topY+1;y<botY;y++)
			vert_edge.pix(x,y) = im_in.pix(x,y);
	}

	

	return vert_edge;
}

