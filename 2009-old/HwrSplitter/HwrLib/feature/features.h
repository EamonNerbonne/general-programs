//+----------------------------------------------------------------------------+
//| Handwriting recognition - Features                                         |
//| Feature extraction from images                                             |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#ifndef FEATURES_H
#define FEATURES_H

// ----------------------------------------------------------------------------- : Includes

#include "../HwrConfig.h"
#include "../image/pamImage.h"
#include "featurevector.h"

using std::vector;

typedef vector<double> floats;

// ----------------------------------------------------------------------------- : High level interface

class ImageFeatures {
private:
	// ----------------------------------------------------------------------------- : Cumsums

	/// The original image; TODO: really shouldn't be here!
	PamImage<BWPixel> image;

	enum CumSums {
		AVG,
		AVG_LOW,
		AVG_MID,
		AVG_HIGH,
	#if USE_DENSITY_DEV
		AVG_SQRT,
		AVG_LOW_SQRT,
		AVG_MID_SQRT,
		AVG_HIGH_SQRT,
	#endif
		AVG_LOW_NEAR,
		AVG_LOW_FAR,
		AVG_HIGH_NEAR,
		AVG_HIGH_FAR,
		AVG_MID_FIXED,
		AVG_HIGH_FIXED_NEAR,
		AVG_HIGH_FIXED_FAR,
		SUM,
#if HORIZ_POS
		SUM_X,
		SUM_XX,
#endif
		SUM_Y,
		SUM_YY,
		DOTCOUNT,
		ANGLE1,
		ANGLE2,
		ANGLE3,
		ANGLE4,
		ANGLENONE,
#if USE_SKELETON
		SANGLE1,
		SANGLE2,
		SANGLE3,
		SANGLE4,
		SANGLENONE, // skeleton angles
#endif
	#if USE_BOUNDARY_ANGLES
		BOUNDARY_HISTO,
		BOUNDARY_HISTO_END = BOUNDARY_HISTO + BOUNDARY_HISTO_RESOLUTION_BASE * BOUNDARY_HISTO_RESOLUTION_DIFF,
	#else
		BOUNDARY_HISTO_END = BANGLENONE,
	#endif
	#if USE_MEAN_ANGLES
#if HORIZ_POS
		SUM_X_HISTO      = BOUNDARY_HISTO_END,
		SUM_X_HISTO_END  = SUM_X_HISTO + BOUNDARY_HISTO_RESOLUTION_BASE,
#else
		SUM_X_HISTO_END,
#endif
		SUM_Y_HISTO      = SUM_X_HISTO_END,
		SUM_Y_HISTO_END  = SUM_Y_HISTO + BOUNDARY_HISTO_RESOLUTION_BASE,
	#else
		SUM_Y_HISTO_END  = BROKEN_BOUNDARY_HISTO_END,
	#endif
	#if USE_DOWNSAMPLED
		DOWNSAMPLED      = SUM_Y_HISTO_END,
		DOWNSAMPLED_END  = DOWNSAMPLED + DOWNSAMPLE_RESOLUTION_Y,
	#else
		DOWNSAMPLED_END  = SUM_Y_HISTO_END,
	#endif
	#if USE_DOWNSAMPLED_BGS
		DOWNSAMPLED_BGS     = DOWNSAMPLED_END,
		DOWNSAMPLED_BGS_END = DOWNSAMPLED_BGS + DOWNSAMPLE_RESOLUTION_Y,
	#else
		DOWNSAMPLED_BGS_END = DOWNSAMPLED_END,
	#endif
	#if USE_RUNLENGTHS
		RUNLENGTH        = DOWNSAMPLED_BGS_END,
		RUNLENGTH_RL     = RUNLENGTH + RUNLENGTH_RESOLUTION_Y,
		RUNLENGTH_END    = RUNLENGTH_RL + RUNLENGTH_RESOLUTION_Y,
	#else
		RUNLENGTH_END    = DOWNSAMPLED_BGS_END,
	#endif
	#if USE_ANGLE_ZONES
		BOUNDARY_HISTO1     = RUNLENGTH_END,
		BOUNDARY_HISTO1_END = BOUNDARY_HISTO1 + BOUNDARY_HISTO_RESOLUTION_BASE,
	#else
		BOUNDARY_HISTO1_END = RUNLENGTH_END,
	#endif
		MAX_CAT = BOUNDARY_HISTO1_END
	};

	floats cumsums[MAX_CAT];
	
	// return "mean(i = x, x < width) cat(i)"
	inline Feature featV(int cat, int x) const;
	void init(int winSizeDens,int winSizeAngle, int iter);
	void blurEm(int winSizeDens,int winSizeAngle, int iter);
	void blurHisto();
	void initRunlength();
public:
		/// Size of the image
	const int image_width, image_height;
	/// Top and baseline
	const int topline, baseline;

	/// Construct an image feature extractor for a word image
	/** The image will not be preprocessed
	*/
	ImageFeatures(PamImage<BWPixel> const& im,int topline, int baseline, int winSizeDens=4,int winSizeAngle=6, int blurIterations=3);
	/// Was the data correctly loaded?
	inline bool ok() const { return image_width >= 0; }

	/// Get the feature value at an x coord
	const FeatureVector& featAt(int x) const;

	/// Write to a binary file
	void save(const std::string& name);
	void save(FILE* file);
	/// Read from a binary file, return success
	bool load(const std::string& name);
	bool load(FILE* file);

	inline int getImageWidth(void) const  {return image_width;}	
};

// ----------------------------------------------------------------------------- : Feature extraction

/// Fraction of non-zero pixels in a certain region
double dark_fraction(PamImage<BWPixel> const& im, int x0 = 0, int y0 = 0, int x1 = INT_MAX, int y1 = INT_MAX);

/// Classification of angle types in a certain region
/// Returns an array [-,/,|,\]
void angle_types(PamImage<BWPixel> const& im, int x0, int y0, int width, int height, double* out);
floats angle_types(PamImage<BWPixel> const& im, int x = 0, int y = 0, int width = INT_MAX, int height = INT_MAX);

/// Statistics:  mean x, mean y, var x, var y
floats mean_variance(PamImage<BWPixel> const& im, int x = 0, int y = 0, int width = INT_MAX, int height = INT_MAX);

///Determine alternate corpus.
void CalcAltTopline(int baseline, int topline, int & altTop, int &altTopNear, int &altTopFar, int &altTopFarEnd);


// ----------------------------------------------------------------------------- : Morphology

/// Morphological dilation
PamImage<BWPixel> dilate(PamImage<BWPixel> const& im, int radius);

/// Morphological erosion
PamImage<BWPixel> erode(PamImage<BWPixel> const& im, int radius);

/// Morphological closing (= erode . dilate)
PamImage<BWPixel> closing(PamImage<BWPixel> const& im, int radius);

// ----------------------------------------------------------------------------- : Floodfill

/// Floodfill away all of the background of an image, starting from the borders
void floodfill_border(PamImage<RGBPixel> const& im);

/// Floodfill away all of the background of an image, starting from the corners
void floodfill_corners(PamImage<RGBPixel> const& im);

//rest
#if _MANAGED
#undef max
#undef min
#endif

template<typename T>
void fastblur(T& data, int dataCnt, int win,int iter, double blurRatio) {
	using namespace boost;
	scoped_array<double> acc(new double[dataCnt]);
	for(int itCnt=0;itCnt<iter;itCnt++) {
		double lastVal=0.0;
		for(int i=0;i<(int)dataCnt;i++) {
			lastVal+=data[i];
			acc[i]=lastVal;
		}
		for(int i=dataCnt-1;i>=0;i--) {
			int i0 = std::max(i-win,0)-1;
			int i1 = std::min(i+win,dataCnt-1);
			double v0 = i0<0?0.0:acc[i0];
			double v1 = acc[i1];
			double avg = (v1-v0)/(i1-i0);
			data[i] = avg*blurRatio + data[i]*(1-blurRatio);
		}
	}
}


// ----------------------------------------------------------------------------- : EOF
#endif
