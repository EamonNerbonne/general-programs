//+----------------------------------------------------------------------------+
//| Handwriting recognition - Features                                         |
//| Feature extraction from images                                             |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes

#include "stdafx.h"
#include "HwrConfig.h"
#include "feature/features.h"
#include "image/preprocess.h"
#include "image/segmentation.h"
#include "image/skeleton.h"
#include "image/envelopes.h"
#include "util/for_each.hpp"
#include <boost/scoped_array.hpp>

using namespace std;

#ifdef _MSC_VER
	const inline int finite(double f) {
		return _finite(f);
	}
#endif

#ifndef ASSERT
#define ASSERT(a)
#endif

DECLARE_TYPEOF_COLLECTION(ImageComponent);

// ----------------------------------------------------------------------------- : Flags

#define USE_RELATIVE_ANGLES     1
#define USE_HISTO_INTERPOLATION 1
#define USE_HISTO_PI            2 * M_PI // whole circle
//#define USE_HISTO_PI            1 * M_PI // half circle, gives slightly worse performance
#define USE_HISTO_SCALE_SIN     0
#define USE_HISTO_SCALE_ARCSIN  0

#define USE_WIDE_EDGES          2 // left/right  edge width


#if USE_SKELETON_DENSITY
#define USE_SKELETON_DENSITY    0
#define USE_SKELETON_MEAN       0
#define USE_SKELETON_DOWNSAMPLE 0
#endif

#define USE_DOWNSAMPLE_COMPRESS 0
#define USE_ENVELOPE_OVERLAP    1

// ----------------------------------------------------------------------------- : Features

// TL + 2*TR + 4*BL + 8*BR  ->  angle type  {-,/,|,\,none}
const int angle_type[] = {4, 1, 3, 0, 3, 2, 1, 1, 1, 3, 2, 3, 0, 3, 1, 4};

FeatureVector features(0.0);

int to_boundary_histo_bin(int size, double angle) {
	int g = (int)floor(angle / (USE_HISTO_PI) * size + 0.5);
	return (g + size) % size;
}
void to_boundary_histo_bin(int size, double angle, int& binA, int& binB, double& rem) {
	double scaled = angle / (USE_HISTO_PI) * size;
	int g = (int)floor(scaled);
	rem = (double)(scaled - g);
	binA = (g +     size) % size;
	binB = (g + 1 + size) % size;
}
// clip bin position
void clipbin(int& bin) {
	int over = BOUNDARY_HISTO_RESOLUTION_DIFF_OVER;
	int clip = BOUNDARY_HISTO_RESOLUTION_DIFF;
	if (clip != over) {
		if (bin < over/2) bin = min(bin,clip/2-1);
		else              bin = max(bin-over+clip,clip/2);
	}
}

ImageFeatures::ImageFeatures(PamImage<BWPixel> const& im, int topline,int baseline, int winSizeDens,int winSizeAngle, int iter)
: image_width(  im.getWidth() )
, image_height( im.getHeight() )
, topline (topline)
, baseline(baseline)
, image(im)
{
	if(baseline <=0)
			find_baseline(image, const_cast<int&>(topline), const_cast<int&>(baseline));

	init(winSizeDens,winSizeAngle, iter);
}

void CalcAltTopline(int baseline, int topline, int & altTop, int &altTopNear, int &altTopFar, int &altTopFarEnd) {
	altTop = max(topline,baseline-38);
	altTopNear = max(altTop * 4/5, altTop - 13);
	altTopFar = max(altTop * 1/5, altTop - 26);
	altTopFarEnd = max(0, altTop - 52);
}

void ImageFeatures::init(int winSizeDens,int winSizeAngle, int iter) {
	using namespace boost;
	ASSERT(sizeof(cumsums) / sizeof(cumsums[0]) == MAX_CAT);
	// Find baseline
	// Derived images
#if USE_DOWNSAMPLED_BGS
	PamImage<BWPixel> imageBS = skeleton(invert(image), 10);
#endif
#if USE_SKELETON
	PamImage<BWPixel> imageS  = skeleton(this->image, 10);
	// Prepare
	PamImage<BWPixel> &pixelsForDensity    = USE_SKELETON_DENSITY    ? imageS : image;
	PamImage<BWPixel> &pixelsForMean       = USE_SKELETON_MEAN       ? imageS : image;
	PamImage<BWPixel> &pixelsForDownsample = USE_SKELETON_DOWNSAMPLE ? imageS : image;
#else
	PamImage<BWPixel> &pixelsForDensity    = image;
	PamImage<BWPixel> &pixelsForMean       = image;
	PamImage<BWPixel> &pixelsForDownsample = image;
#endif

	for (int cat = 0 ; cat < MAX_CAT ; cat++) {
		cumsums[cat].resize(image_width + 1);
	}
	
	int altTop, altTopNear, altTopFar, altTopFarEnd;
	CalcAltTopline(baseline, topline, altTop, altTopNear, altTopFar, altTopFarEnd);
	
	// Accumulate feature values : separate
	double weight  = 1.0f / topline;
	double weight2 = 2.0f / (topline * (1 + topline));
	for (int x = 0 ; x < image_width ; ++x) {
		for (int y = 0 ; y < topline ; ++y) {
			cumsums[AVG_HIGH     ][x] += pixelsForDensity.pix(x,y) * weight;
			cumsums[AVG_HIGH_NEAR][x] += pixelsForDensity.pix(x,y) * (topline - y) * weight2;
			cumsums[AVG_HIGH_FAR ][x] += pixelsForDensity.pix(x,y) * (y + 1) * weight2;
		}
		#if USE_DENSITY_DEV
			cumsums[AVG_HIGH_SQRT][x]  = sqrt(cumsums[AVG_HIGH][x]);
		#endif
	}
	weight = 1.0f / (baseline - topline);
	for (int x = 0 ; x < image_width ; ++x) {
		for (int y = topline ; y < baseline ; ++y) {
			cumsums[AVG_MID][x] += pixelsForDensity.pix(x,y) * weight;
		}
		#if USE_DENSITY_DEV
			cumsums[AVG_MID_SQRT][x]  = sqrt(cumsums[AVG_MID][x]);
		#endif
	}
	weight  = 1.0f / (image_height - baseline);
	weight2 = 2.0f / ((image_height - baseline) * (1 + (image_height - baseline)));
	for (int x = 0 ; x < image_width ; ++x) {
		for (int y = baseline ; y < image_height ; ++y) {
			cumsums[AVG_LOW     ][x] += pixelsForDensity.pix(x,y) * weight;
			cumsums[AVG_LOW_NEAR][x] += pixelsForDensity.pix(x,y) * (y - baseline + 1) * weight2;
			cumsums[AVG_LOW_FAR ][x] += pixelsForDensity.pix(x,y) * (image_height - y) * weight2;
		}
		#if USE_DENSITY_DEV
			cumsums[AVG_LOW_SQRT][x]  = sqrt(cumsums[AVG_LOW][x]);
		#endif
	}
	// Accumulate feature values : fixed
	weight  = 1.0f / (baseline-altTop);
	for (int y = altTop ; y < baseline ; ++y) {
		for (int x = 0 ; x < image_width ; ++x) {
			cumsums[AVG_MID_FIXED][x] += pixelsForDensity.pix(x,y) * weight;
		}
	}
	weight  = 1.0f / (altTop-altTopNear);
	for (int y = altTopNear ; y < altTop ; ++y) {
		for (int x = 0 ; x < image_width ; ++x) {
			cumsums[AVG_HIGH_FIXED_NEAR][x] += pixelsForDensity.pix(x,y) * weight;
		}
	}
	weight  = 1.0f / (altTopFar-altTopFarEnd);
	for (int y = altTopFarEnd ; y < altTopFar ; ++y) {
		for (int x = 0 ; x < image_width ; ++x) {
			cumsums[AVG_HIGH_FIXED_FAR][x] += pixelsForDensity.pix(x,y) * weight;
		}
	}
	// Accumulate feature values : over all lines
	weight = 1.0f / image_height;
	for (int x = 0 ; x < image_width ; ++x) {
		for (int y = 0 ; y < image_height ; ++y) {
			cumsums[AVG][x]    += pixelsForDensity.pix(x,y) * weight;
			cumsums[SUM][x]    += pixelsForMean.pix(x,y) * weight;
			cumsums[SUM_Y][x]  += y * pixelsForMean.pix(x,y) * weight;
			cumsums[SUM_YY][x] += y * y * pixelsForMean.pix(x,y) * weight;
#if HORIZ_POS
			cumsums[SUM_X][x]  += x * pixelsForMean.pix(x,y) * weight;
			cumsums[SUM_XX][x] += x * x * pixelsForMean.pix(x,y) * weight;
#endif
		}
		#if USE_DENSITY_DEV
			cumsums[AVG_SQRT][x]  = sqrt(cumsums[AVG][x]);
		#endif
	}
	
	// Accumulate feature values : downsampled
	double total = 1e-10, total_bgs = 1e-10;
	for (int y = 0 ; y < image_height ; ++y) {
		for (int x = 0 ; x < image_width ; ++x) {
			total += pixelsForDownsample.pix(x,y);
			#if USE_DOWNSAMPLED_BGS
				total_bgs += imageBS.pix(x,y);
			#endif
		}
	}
	weight = image_width / total;
	#if USE_DOWNSAMPLED_BGS
		weight2 = image_width / total_bgs;
	#endif
	ASSERT(DOWNSAMPLE_BGS_RESOLUTION_Y == DOWNSAMPLE_RESOLUTION_Y);
	int rows[DOWNSAMPLE_RESOLUTION_Y+1];
	if (DOWNSAMPLE_RESOLUTION_Y == 9) {
		rows[0] = 0;
		rows[1] = (2*0        + 1*topline     )/3;
		rows[2] = (1*0        + 2*topline     )/3;
		rows[3] = topline;
		rows[4] = (2*topline  + 1*baseline    )/3;
		rows[5] = (1*topline  + 2*baseline    )/3;
		rows[6] = baseline;
		rows[7] = (2*baseline + 1*image_height)/3;
		rows[8] = (1*baseline + 2*image_height)/3;
		rows[9] = image_height;
	} else if (DOWNSAMPLE_RESOLUTION_Y == 6) {
		rows[0] = 0;
		rows[1] = (1*0        + 1*topline     )/2;
		rows[2] = topline;
		rows[3] = (1*topline  + 1*baseline    )/2;
		rows[4] = baseline;
		rows[5] = (1*baseline + 1*image_height)/2;
		rows[6] = image_height;
	} else {
		throw "unsupported DOWNSAMPLE_RESOLUTION_Y";
	}
	for (int u = 0 ; u < DOWNSAMPLE_RESOLUTION_Y ; ++u) {
		for (int y = rows[u] ; y < rows[u+1] ; ++y) {
			for (int x = 0 ; x < image_width ; ++x) {
				cumsums[DOWNSAMPLED + u][x] += pixelsForDownsample.pix(x,y) * weight;
				#if USE_DOWNSAMPLED_BGS
					cumsums[DOWNSAMPLED_BGS + u][x] += imageBS.px().pix(x,y) * weight2;
				#endif
			}
		}
	}
	
	// Accumulate feature values : not touching edges
	for (int y = 0 ; y + 1 < image_height ; ++y) {
		for (int x = 0 ; x + 1 < image_width ; ++x) {
			int a = angle_type[ image.pix(x,y) + 2 * image.pix(x+1,y) + 4 * image.pix(x,y+1) + 8 * image.pix(x+1,y+1) ];
			if (a >= 0) cumsums[ANGLE1 + a][x] += 1;
#if USE_SKELETON
			a = angle_type[ imageS.pix(x,y) + 2 * imageS.pix(x+1,y) + 4 * imageS.pix(x,y+1) + 8 * imageS.pix(x+1,y+1) ];
			if (a >= 0) cumsums[SANGLE1 + a][x] += 1;
#endif
		}
	}
	// normalize angle types, avg. will be 1 over each column
	for (int t = 0 ; t < 3 ; ++t) {
		double tot = 1;
		for (int x = 0 ; x + 1 < image_width ; ++x) {
			for (int a = 0 ; a < 4 ; ++a) tot += cumsums[ANGLE1+a+5*t][x];
		}
		double factor = image_width / tot;
		for (int x = 0 ; x + 1 < image_width ; ++x) {
			for (int a = 0 ; a < 5 ; ++a) cumsums[ANGLE1+a+5*t][x] *= factor;
		}
	}
	
	// Accumulate feature values : boundary histogram
	vector<Boundary> boundaries;
	::boundaries(image, boundaries);
	int b_len = 0;
	for (size_t i = 0 ; i < boundaries.size() ; ++i) {
		b_len += boundaries[i].size();
	}
	weight = (double)image_width / b_len; // avg over columns will be 1
	for (size_t i = 0 ; i < boundaries.size() ; ++i) {
		Boundary& b = boundaries[i];
		for (int j = 0 ; j < b.size() ; ++j) {
			double a1 = b.angle(j - 5, j);
			double a2 = b.angle(j, j + 5);
			if (USE_RELATIVE_ANGLES) {
				a2 -= a1;
				#if USE_HISTO_SCALE_SIN
					a2 = sin(a2 / 2) * M_PI;
				#elif USE_HISTO_SCALE_ARCSIN
					a2 = asin(a2 / M_PI) * 2;
				#endif
			}
			#if USE_HISTO_INTERPOLATION
				// interpolation over bins
				int bin1a, bin1b, bin2a, bin2b;
				double rem1, rem2;
				to_boundary_histo_bin(BOUNDARY_HISTO_RESOLUTION_BASE,      a1, bin1a, bin1b, rem1);
				to_boundary_histo_bin(BOUNDARY_HISTO_RESOLUTION_DIFF_OVER, a2, bin2a, bin2b, rem2);
				clipbin(bin2a);
				clipbin(bin2b);
				int x = b[j].x;
				int y = b[j].y;
				int amount = x == 0 || x+1 == image_width ? 2 : 1;
					int max_xx = min(image_width,x+1);
				for (int xx = max(0,x - 1) ; xx < max_xx ; ++xx) {
					#if USE_BOUNDARY_ANGLES
						cumsums[BOUNDARY_HISTO + bin1a * BOUNDARY_HISTO_RESOLUTION_DIFF + bin2a][xx] += amount * weight * (1-rem1) * (1-rem2);
						cumsums[BOUNDARY_HISTO + bin1a * BOUNDARY_HISTO_RESOLUTION_DIFF + bin2b][xx] += amount * weight * (1-rem1) * (  rem2);
						cumsums[BOUNDARY_HISTO + bin1b * BOUNDARY_HISTO_RESOLUTION_DIFF + bin2a][xx] += amount * weight * (  rem1) * (1-rem2);
						cumsums[BOUNDARY_HISTO + bin1b * BOUNDARY_HISTO_RESOLUTION_DIFF + bin2b][xx] += amount * weight * (  rem1) * (  rem2);
					#endif
					#if USE_MEAN_ANGLES
#if HORIZ_POS
						cumsums[SUM_X_HISTO + bin1a][xx] += xx * amount * weight * (1-rem1);
						cumsums[SUM_X_HISTO + bin1b][xx] += xx * amount * weight *    rem1;
#endif
						cumsums[SUM_Y_HISTO + bin1a][xx] += y  * amount * weight * (1-rem1);
						cumsums[SUM_Y_HISTO + bin1b][xx] += y  * amount * weight *    rem1;
					#endif
				}
				// single dimension
				#if USE_ANGLE_ZONES
					cumsums[BOUNDARY_HISTO1 + bin1a][x] += amount * weight * (1-rem1);
					cumsums[BOUNDARY_HISTO1 + bin1b][x] += amount * weight *    rem1;
				#endif
				// also the sum x / sum y
			#else
				int bin_id = BOUNDARY_HISTO
					+ to_boundary_histo_bin(BOUNDARY_HISTO_RESOLUTION_BASE, a1) * BOUNDARY_HISTO_RESOLUTION_DIFF
					+ to_boundary_histo_bin(BOUNDARY_HISTO_RESOLUTION_DIFF, a2);
				int x = b[j].x;
				if (x < image_width) cumsums[bin_id][x  ] += weight;
				if (x > 0)           cumsums[bin_id][x-1] += weight;
			#endif
		}
	}
	#if USE_DOT_DETECTOR
		for (int x = 0 ; x < image_width ; ++x) {
			cumsums[DOTCOUNT][x] =0.0;
		}
		const int MAX_DOT_SIZE = 50;
		
		SegmentedImage seg_image( image );
		FOR_EACH(c, seg_image.components) {
			if (c.size < MAX_DOT_SIZE && c.max_y < (topline + baseline)/2) {
				double dot_size= (double)1.5 - max((double)0.0, (double)c.size / MAX_DOT_SIZE - (double)0.5);
				cumsums[DOTCOUNT][(c.max_x+c.min_x)/2] = dot_size;
			}
		}
	#endif

	//blurHisto(); // blurring makes things worse
	initRunlength();
#if FEATURE_BLUR
	blurEm(winSizeDens,winSizeAngle,iter); //but blurring in x makes thing better.
#endif
}

void ImageFeatures::initRunlength() {
  #if USE_RUNLENGTHS
	assert(RUNLENGTH_RESOLUTION_Y == 5);
	int rows[RUNLENGTH_RESOLUTION_Y] =
		{/*0,        /*/(0        + topline     )/2,/**/
		 topline,  (topline  + baseline    )/2,
		 baseline, /**/(baseline + image_height)/2/*/
		 image_height/**/
		};
	// for each row...
	for (int row = 0 ; row < RUNLENGTH_RESOLUTION_Y ; ++row) {
		int yy = rows[row];
		// left to right pass
		int run = 0;
		for (int x = 0 ; x < image_width ; ++x) {
			int ink = 0, non_ink = 0;
			for (int y = max(0,yy-2) ; y < min(image_height-1,yy+2) ; ++y) {
				if (image.pix(x,y)) ink++;
				else              non_ink++;
			}
			if (ink * 2 >= non_ink) {
				run = 0;
			} else {
				run++;
			}
			cumsums[RUNLENGTH + row][x] = run;
		}
	}
	for (int row = 0 ; row < RUNLENGTH_RESOLUTION_Y ; ++row) {
		int yy = rows[row];
		// left to right pass
		int run = 0;
		for (int x = image_width-1 ; x >=0 ; x--) {
			int ink = 0, non_ink = 0;
			for (int y = max(0,yy-2) ; y < min(image_height-1,yy+2) ; ++y) {
				if (image.pix(x,y)) ink++;
				else              non_ink++;
			}
			if (ink * 2 >= non_ink) {
				run = 0;
			} else {
				run++;
			}
			cumsums[RUNLENGTH_RL + row][x] = run;
		}
	}
	// TODO: also right to left?
  #endif
}

const FeatureVector& ImageFeatures::featAt(int x) const {
	// features
#if HORIZ_POS
	features[FEATURE_IMAGE_WIDTH]        = (Feature)image_width;
	features[FEATURE_IMAGE_HEIGHT]       = (Feature)image_height;
	features[FEATURE_WINDOW_WIDTH]       = (Feature)(x2 - x1);
	features[FEATURE_X_HEIGHT]           = (Feature)(baseline - topline);
#endif

	//variance is E(X^2) - E(X)^2, so stddev of sqrt(dens) is  sqrt( E(dens) - E(sqrt(dens))^2)
	features[FEATURE_DENSITY]            = featV(AVG,      x);
	features[FEATURE_DENSITY_LOW]        = featV(AVG_LOW,  x);
	features[FEATURE_DENSITY_MID]        = featV(AVG_MID,  x);
	features[FEATURE_DENSITY_HIGH]       = featV(AVG_HIGH, x);
	
	#if USE_DENSITY_DEV
		features[FEATURE_DENSITY_DEV]        = sqrt(max((double)0.0,features[FEATURE_DENSITY]      - sqr(featV(AVG_SQRT,x))));
		features[FEATURE_DENSITY_LOW_DEV]    = sqrt(max((double)0.0,features[FEATURE_DENSITY_LOW]  - sqr(featV(AVG_LOW_SQRT,x))));
		features[FEATURE_DENSITY_MID_DEV]    = sqrt(max((double)0.0,features[FEATURE_DENSITY_MID]  - sqr(featV(AVG_MID_SQRT,x))));
		features[FEATURE_DENSITY_HIGH_DEV]   = sqrt(max((double)0.0,features[FEATURE_DENSITY_HIGH] - sqr(featV(AVG_HIGH_SQRT,x))));
	#endif
	
	features[FEATURE_DENSITY_LOW_NEAR]   = featV(AVG_LOW_NEAR,  x);
	features[FEATURE_DENSITY_LOW_FAR]    = featV(AVG_LOW_FAR,   x);
	features[FEATURE_DENSITY_HIGH_NEAR]  = featV(AVG_HIGH_NEAR, x);
	features[FEATURE_DENSITY_HIGH_FAR]   = featV(AVG_HIGH_FAR,  x);

	features[FEATURE_DENSITY_MID_FIX]       = featV(AVG_MID_FIXED,  x);
	features[FEATURE_DENSITY_HIGH_NEAR_FIX] = featV(AVG_HIGH_FIXED_NEAR,  x);
	features[FEATURE_DENSITY_HIGH_FAR_FIX]  = featV(AVG_HIGH_FIXED_FAR,  x);
	features[FEATURE_DENSITY_HIGH_MIN_FIX]  = min(features[FEATURE_DENSITY_HIGH_NEAR_FIX], features[FEATURE_DENSITY_HIGH_FAR_FIX]);

	#if USE_EDGES
	features[FEATURE_DENSITY_LEFT_EDGE]  = featV(AVG_MID,  max(0,x1-USE_WIDE_EDGES), min(image_width,x1+USE_WIDE_EDGES)); // in the edge
	features[FEATURE_DENSITY_RIGHT_EDGE] = featV(AVG_MID,  max(0,x2-USE_WIDE_EDGES), min(image_width,x2+USE_WIDE_EDGES)); // in the edge
	#endif
#if HORIZ_POS
	features[FEATURE_DENSITY_LEFT_LOW]   = featV(AVG_LOW,  x1,(2*x1+x2+2)/3); // left - we add +2 to ensure it's greater than x1.
	features[FEATURE_DENSITY_LEFT_MID]   = featV(AVG_MID,  x1,(2*x1+x2+2)/3);
	features[FEATURE_DENSITY_LEFT_HIGH]  = featV(AVG_HIGH, x1,(2*x1+x2+2)/3);
	features[FEATURE_DENSITY_RIGHT_LOW]  = featV(AVG_LOW,  (x1+2*x2)/3,x2); // right
	features[FEATURE_DENSITY_RIGHT_MID]  = featV(AVG_MID,  (x1+2*x2)/3,x2);
	features[FEATURE_DENSITY_RIGHT_HIGH] = featV(AVG_HIGH, (x1+2*x2)/3,x2);
#endif
	double density = max(featV(SUM, x), (double)1e-10);
	double mean_y = featV(SUM_Y, x) / density;
#if HORIZ_POS
	double mean_x = featV(SUM_X, x) / density;
	features[FEATURE_MEAN_X_WORD]     = mean_x / image_width;
	features[FEATURE_MEAN_X_WINDOW]   = (mean_x - x1) / (x2-x1);
	features[FEATURE_STDDEV_X_WINDOW] = sqrt(max((double)0, featV(SUM_XX, x) / density - sqr(mean_x))) / image_width;  // std deviation
#endif
	features[FEATURE_MEAN_Y_WINDOW]   = (mean_y - baseline) / image_height;//baseline has more meaning.
	features[FEATURE_STDDEV_Y_WINDOW] = sqrt(max((double)0, featV(SUM_YY, x) / density - sqr(mean_y))) / image_height;
	
	#if USE_DOT_DETECTOR
		features[FEATURE_DOT_COUNT] = featV(DOTCOUNT,   x);
	#endif
	
	features[FEATURE_BOUNDARY_ANGLE_2PIX_1]   = featV(ANGLE1,   x);
	features[FEATURE_BOUNDARY_ANGLE_2PIX_2]   = featV(ANGLE2,   x);
	features[FEATURE_BOUNDARY_ANGLE_2PIX_3]   = featV(ANGLE3,   x);
	features[FEATURE_BOUNDARY_ANGLE_2PIX_4]   = featV(ANGLE4,   x);
	features[FEATURE_BOUNDARY_ANGLE_2PIX_NONE]   = featV(ANGLENONE,   x);
#if USE_SKELETON
	features[FEATURE_SKELETON_ANGLE_2PIX_1]   = featV(SANGLE1,  x);
	features[FEATURE_SKELETON_ANGLE_2PIX_2]   = featV(SANGLE2,  x);
	features[FEATURE_SKELETON_ANGLE_2PIX_3]   = featV(SANGLE3,  x);
	features[FEATURE_SKELETON_ANGLE_2PIX_4]   = featV(SANGLE4,  x);
	features[FEATURE_SKELETON_ANGLE_2PIX_NONE]   = featV(SANGLENONE,  x);
#endif	
	#if USE_BOUNDARY_ANGLES
		for (int j = 0 ; j < BOUNDARY_HISTO_RESOLUTION_BASE * BOUNDARY_HISTO_RESOLUTION_DIFF ; ++j) {
			features[FEATURE_BOUNDARY_ANGLE_HISTO     + j] = featV(BOUNDARY_HISTO + j, x);
#if HORIZ_POS
			features[FEATURE_BOUNDARY_ANGLE_HISTO_MID + j] = featV(BOUNDARY_HISTO + j, (3*x1+x2)/4,(x1+3*x2+3)/4);
#endif
		}
	#endif
	for (int j = 0 ; j < BOUNDARY_HISTO_RESOLUTION_BASE ; ++j) {
		#if USE_MEAN_ANGLES
#if HORIZ_POS
			features[FEATURE_MEAN_X_ANGLE_HISTO + j] = (featV(SUM_X_HISTO + j, x) - x1) / (x2-x1);
#endif
			features[FEATURE_MEAN_Y_ANGLE_HISTO + j] = (featV(SUM_Y_HISTO + j, x) - baseline) / image_height;
		#endif
	}
	
	#if USE_DOWNSAMPLED
		for (int y = 0 ; y < DOWNSAMPLE_RESOLUTION_Y ; ++y) {
			features[FEATURE_DOWNSAMPLED + y ] = featV(DOWNSAMPLED + y, x);
		}
	#endif
	#if USE_DOWNSAMPLED_BGS
		for (int y = 0 ; y < DOWNSAMPLE_BGS_RESOLUTION_Y ; ++y) {
			features[FEATURE_DOWNSAMPLED_BGS + y ] = featV(DOWNSAMPLED_BGS + y, x);
		}
	#endif
	
	#if USE_RUNLENGTHS
		for (int y = 0 ; y < RUNLENGTH_RESOLUTION_Y ; ++y) {
			features[FEATURE_RUNLENGTH + y ] =  featV(RUNLENGTH + y, x);
			features[FEATURE_RUNLENGTH_RL + y ] =  featV(RUNLENGTH_RL + y, x);
		}
	#endif
	
	#if USE_ENVELOPES
		// I am too lazy to use cumsums
		vector<int> lenv = lower_envelope(image);
		vector<int> uenv = upper_envelope(image);
		for (int i = 0 ; i < ENVELOPE_RESOLUTION_X ; ++i) {
			#if USE_ENVELOPE_OVERLAP
				int left  = max(x1 + (int)((i-.5) *(x2-x1)/ENVELOPE_RESOLUTION_X) - 1, 0);
				int right = min(x1 + (int)((i+1.5)*(x2-x1)/ENVELOPE_RESOLUTION_X) + 1, image_width);
			#else
				int left  = max(x1 + i    *(x2-x1)/ENVELOPE_RESOLUTION_X - 1, 0);
				int right = min(x1 + (i+1)*(x2-x1)/ENVELOPE_RESOLUTION_X + 1, image_width);
			#endif
			int l_min = INT_MAX, l_max = 0;
			int u_min = INT_MAX, u_max = 0;
			for (int x = left ; x < right ; ++x) {
				l_min = min(l_min, lenv[x]);
				u_min = min(u_min, uenv[x]);
				l_max = max(l_max, lenv[x]);
				u_max = max(u_max, uenv[x]);
			}
			features[FEATURE_UPPER_ENVELOPE1 + i] = (double)u_min / image_height;
			features[FEATURE_LOWER_ENVELOPE1 + i] = (double)l_min / image_height;
			features[FEATURE_UPPER_ENVELOPE2 + i] = (double)u_max / image_height;
			features[FEATURE_LOWER_ENVELOPE2 + i] = (double)l_max / image_height;
		}
	#endif
	
	#if USE_ANGLE_ZONES
		for (int x = 0 ; x < DOWNSAMPLE_RESOLUTION_X ; ++x) {
			int left  = max(x1 + x    *(x2-x1)/DOWNSAMPLE_RESOLUTION_X - 1, 0);
			int right = min(x1 + (x+1)*(x2-x1)/DOWNSAMPLE_RESOLUTION_X + 1, image_width);
			for (int t = 0 ; t < BOUNDARY_HISTO_RESOLUTION_BASE ; ++t) {
				features[FEATURE_ANGLE_ZONES + t * DOWNSAMPLE_RESOLUTION_X + x] = featV(BOUNDARY_HISTO1 + t, left, right);
			}
		}
	#endif
	
	int index = FEATURE_DENSITY_CORR;
	for (int featA=SELECT_CORR_BEGIN;featA<SELECT_CORR_END;featA++){
		for(int featB=featA+1;featB<SELECT_CORR_END;featB++) {
			features[index++] = features[featA]-features[featB];
		}
	}

#ifndef NDEBUG
	if(index!=FEATURE_DENSITY_CORR_END) {
		cerr<<"whoops, basic logic error, counted correlations wrongly.";
		throw 1;
	}
#endif
#ifndef NDEBUG
	// Check for NaN and Inf
	for(int i=0 ; i<features.size ; i++){ //TODO: we're going to need to approach NaN and Inf a little more sanely sometime.
		if(!finite(features[i])) {
			cerr<<"Illegal number detected in feature #" << i <<endl;
			throw "Illegal feature value";
		}
	}
#endif
	return features;
}

inline Feature ImageFeatures::featV(int cat, int x) const  {
	return  cumsums[cat][x];
}



void ImageFeatures::blurEm(int winSizeDens,int winSizeAngle, int iter) {
	if (!USE_BLUR_FEATURES_X) return;
	for (int cat = 0 ; cat < ANGLE1 ; cat++) 
		fastblur(cumsums[cat],(int)cumsums[cat].size(), winSizeDens,iter,0.8);
	for (int cat = ANGLE1 ; cat < MAX_CAT ; cat++) 
		fastblur(cumsums[cat],(int)cumsums[cat].size(),winSizeAngle,iter,0.8);
}


void ImageFeatures::blurHisto() {
#if USE_BOUNDARY_ANGLES
	for (int x = 0 ; x < image_width ; ++x) {
		// temporary copy
		double boundary_histo[BOUNDARY_HISTO_RESOLUTION_BASE * BOUNDARY_HISTO_RESOLUTION_DIFF];
		for (int i = 0 ; i < BOUNDARY_HISTO_RESOLUTION_BASE * BOUNDARY_HISTO_RESOLUTION_DIFF; ++i) {
			boundary_histo[i] = cumsums[BOUNDARY_HISTO + i][x];
		}
		// quick and dirty blur
		#define boundaryhisto(i,j) \
					boundary_histo[ ((i + BOUNDARY_HISTO_RESOLUTION_BASE) % BOUNDARY_HISTO_RESOLUTION_BASE) * BOUNDARY_HISTO_RESOLUTION_DIFF \
					              + ((j + BOUNDARY_HISTO_RESOLUTION_DIFF) % BOUNDARY_HISTO_RESOLUTION_DIFF)]
		for (int i = 0 ; i < BOUNDARY_HISTO_RESOLUTION_BASE; ++i) {
			for (int j = 0 ; j < BOUNDARY_HISTO_RESOLUTION_DIFF; ++j) {
				cumsums[BOUNDARY_HISTO + i * BOUNDARY_HISTO_RESOLUTION_DIFF + j][x]
					= boundaryhisto(i,  j  ) * (double)0.5
					+ boundaryhisto(i-1,j  ) * (double)0.125
					+ boundaryhisto(i+1,j  ) * (double)0.125
					+ boundaryhisto(i,  j-1) * (double)0.125
					+ boundaryhisto(i,  j+1) * (double)0.125;
			}
		}
		#undef boundarhisto
	}
#endif
}

// ----------------------------------------------------------------------------- : File IO

void ImageFeatures::save(FILE* file) {
	fwrite(&image_width,  sizeof(int), 1, file);
	fwrite(&image_height, sizeof(int), 1, file);
	fwrite(&topline,      sizeof(int), 1, file);
	fwrite(&baseline,     sizeof(int), 1, file);
	for (int cat = 0 ; cat < MAX_CAT ; cat++) {
		fwrite(&cumsums[cat].front(), sizeof(double), image_width+1, file);
	}
}

bool ImageFeatures::load(FILE* file) {
	fread(const_cast<int*>(&image_width),  sizeof(int), 1, file);
	fread(const_cast<int*>(&image_height), sizeof(int), 1, file);
	fread(const_cast<int*>(&topline),      sizeof(int), 1, file);
	fread(const_cast<int*>(&baseline),     sizeof(int), 1, file);
	for (int cat = 0 ; cat < MAX_CAT ; cat++) {
		cumsums[cat].resize(image_width + 1);
		fread(&cumsums[cat].front(), sizeof(double), image_width+1, file);
		if (feof(file)) return false;
	}
	int dummy;
	if (fread(&dummy,1,1,file)) {
		return false;
	}
	return true;
}

void ImageFeatures::save(const std::string& name) {
	FILE* file = fopen(name.c_str(), "wb");
	if (!file) return;
	save(file);
	fclose(file);
}
bool ImageFeatures::load(const std::string& name) {
	FILE* file = fopen(name.c_str(), "rb");
	if (!file) return false;
	bool ok = load(file);
	fclose(file);
	return ok;
}
