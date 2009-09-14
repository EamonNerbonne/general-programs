//+----------------------------------------------------------------------------+
//| Handwriting recognition - Features                                         |
//| Feature vector type                                                        |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#ifndef FEATUREVECTOR_H
#define FEATUREVECTOR_H

// ----------------------------------------------------------------------------- : Includes

#include "../HwrConfig.h"

// ----------------------------------------------------------------------------- : Feature names

#define HORIZ_POS 0

#define USE_EDGES           0
#define USE_SKELETON		0
#define USE_BOUNDARY_ANGLES 1
#define USE_MEAN_ANGLES     1
#define USE_DOWNSAMPLED     1
#define USE_DOWNSAMPLED_BGS 0
#define USE_BLUR_FEATURES_X 1
#define USE_RUNLENGTHS 1 //buggy --- no more.
#define USE_ENVELOPES    0 //very slow
#define USE_DOT_DETECTOR    1

#define USE_ANGLE_ZONES 0

const int BOUNDARY_HISTO_RESOLUTION_BASE = 12; // base angle
const int BOUNDARY_HISTO_RESOLUTION_DIFF = 8;  // angle difference
const int BOUNDARY_HISTO_RESOLUTION_DIFF_OVER = 12; // over-multiply angle difference
const int DOWNSAMPLE_RESOLUTION_Y = 9;
const int DOWNSAMPLE_BGS_RESOLUTION_Y = 9;
const int RUNLENGTH_RESOLUTION_Y = 5;
#if HORIZ_POS
#define USE_DENSITY_DEV 1
const int DOWNSAMPLE_BGS_RESOLUTION_X = 1;
const int RUNLENGTH_RESOLUTION_X = 7;
const int DOWNSAMPLE_RESOLUTION_X = 7;
const int ENVELOPE_RESOLUTION_X = 8;
#endif

enum FeatureTypes
{
	FEATURE_ZERO
#if HORIZ_POS

,	FEATURE_IMAGE_WIDTH = FEATURE_ZERO
,	FEATURE_IMAGE_HEIGHT
,	FEATURE_WINDOW_WIDTH
,	FEATURE_X_HEIGHT
,	FEATURE_DENSITY
#else
,	FEATURE_DENSITY = FEATURE_ZERO
#endif
#if USE_DENSITY_DEV
,	FEATURE_DENSITY_DEV
,	FEATURE_DENSITY_LOW_DEV
,	FEATURE_DENSITY_MID_DEV
,	FEATURE_DENSITY_HIGH_DEV
#endif
,	SELECT_CORR_BEGIN
,	FEATURE_DENSITY_LOW = SELECT_CORR_BEGIN
,	FEATURE_DENSITY_MID
,	FEATURE_DENSITY_HIGH
#if USE_EDGES
,	FEATURE_DENSITY_LEFT_EDGE
,	FEATURE_DENSITY_RIGHT_EDGE
#endif
,	SELECT_CORR_END
,	FEATURE_DENSITY_CORR = SELECT_CORR_END
,	FEATURE_DENSITY_CORR_END = FEATURE_DENSITY_CORR + (SELECT_CORR_END-SELECT_CORR_BEGIN)*(SELECT_CORR_END-SELECT_CORR_BEGIN-1)/2
,	FEATURE_DENSITY_LOW_NEAR= FEATURE_DENSITY_CORR_END
,	FEATURE_DENSITY_LOW_FAR
#if HORIZ_POS
,	FEATURE_DENSITY_LEFT_LOW 
,	FEATURE_DENSITY_LEFT_MID
,	FEATURE_DENSITY_LEFT_HIGH
,	FEATURE_DENSITY_RIGHT_LOW
,	FEATURE_DENSITY_RIGHT_MID
,	FEATURE_DENSITY_RIGHT_HIGH
#endif
,	FEATURE_DENSITY_HIGH_NEAR
,	FEATURE_DENSITY_HIGH_FAR
,	FEATURE_DENSITY_MID_FIX
,	FEATURE_DENSITY_HIGH_NEAR_FIX
,	FEATURE_DENSITY_HIGH_FAR_FIX
,	FEATURE_DENSITY_HIGH_MIN_FIX
#if HORIZ_POS
,	FEATURE_MEAN_X_WORD    // left of word is 0, right is 1
,	FEATURE_MEAN_X_WINDOW  // left of window is 0, right is 1
#endif
,	FEATURE_MEAN_Y_WINDOW
#if HORIZ_POS
,	FEATURE_STDDEV_X_WINDOW
#endif
,	FEATURE_STDDEV_Y_WINDOW
#if USE_DOT_DETECTOR
,	FEATURE_DOT_COUNT
#endif
,	FEATURE_BOUNDARY_ANGLE_2PIX_1
,	FEATURE_BOUNDARY_ANGLE_2PIX_2
,	FEATURE_BOUNDARY_ANGLE_2PIX_3
,	FEATURE_BOUNDARY_ANGLE_2PIX_4
,	FEATURE_BOUNDARY_ANGLE_2PIX_NONE
#if USE_SKELETON
,	FEATURE_SKELETON_ANGLE_2PIX_1
,	FEATURE_SKELETON_ANGLE_2PIX_2
,	FEATURE_SKELETON_ANGLE_2PIX_3
,	FEATURE_SKELETON_ANGLE_2PIX_4
,	FEATURE_SKELETON_ANGLE_2PIX_NONE
#endif
#if USE_BOUNDARY_ANGLES
,	FEATURE_BOUNDARY_ANGLE_HISTO
,	FEATURE_BOUNDARY_ANGLE_HISTO_END = FEATURE_BOUNDARY_ANGLE_HISTO + BOUNDARY_HISTO_RESOLUTION_BASE * BOUNDARY_HISTO_RESOLUTION_DIFF
#if HORIZ_POS
,	FEATURE_BOUNDARY_ANGLE_HISTO_MID     = FEATURE_BOUNDARY_ANGLE_HISTO_END
,	FEATURE_BOUNDARY_ANGLE_HISTO_MID_END = FEATURE_BOUNDARY_ANGLE_HISTO_MID + BOUNDARY_HISTO_RESOLUTION_BASE * BOUNDARY_HISTO_RESOLUTION_DIFF
#else
,	FEATURE_BOUNDARY_ANGLE_HISTO_MID_END = FEATURE_BOUNDARY_ANGLE_HISTO_END
#endif
#else
,	FEATURE_BOUNDARY_ANGLE_HISTO_MID_END 
#endif

#if USE_MEAN_ANGLES
#if HORIZ_POS
,	FEATURE_MEAN_X_ANGLE_HISTO     = FEATURE_BOUNDARY_ANGLE_HISTO_MID_END
,	FEATURE_MEAN_X_ANGLE_HISTO_END = FEATURE_MEAN_X_ANGLE_HISTO + BOUNDARY_HISTO_RESOLUTION_BASE
#else
,	FEATURE_MEAN_X_ANGLE_HISTO_END = FEATURE_BOUNDARY_ANGLE_HISTO_MID_END
#endif
,	FEATURE_MEAN_Y_ANGLE_HISTO     = FEATURE_MEAN_X_ANGLE_HISTO_END
,	FEATURE_MEAN_Y_ANGLE_HISTO_END = FEATURE_MEAN_Y_ANGLE_HISTO + BOUNDARY_HISTO_RESOLUTION_BASE
#else
,	FEATURE_MEAN_Y_ANGLE_HISTO_END = FEATURE_BOUNDARY_ANGLE_HISTO_MID_END
#endif

#if USE_DOWNSAMPLED
,	FEATURE_DOWNSAMPLED     = FEATURE_MEAN_Y_ANGLE_HISTO_END
,	FEATURE_DOWNSAMPLED_END = FEATURE_DOWNSAMPLED + DOWNSAMPLE_RESOLUTION_Y 
#if HORIZ_POS
													* DOWNSAMPLE_RESOLUTION_X
#endif
#else
,	FEATURE_DOWNSAMPLED_END = FEATURE_MEAN_Y_ANGLE_HISTO_END
#endif
#if USE_DOWNSAMPLED_BGS
,	FEATURE_DOWNSAMPLED_BGS     = FEATURE_DOWNSAMPLED_END
,	FEATURE_DOWNSAMPLED_BGS_END = FEATURE_DOWNSAMPLED_BGS + DOWNSAMPLE_BGS_RESOLUTION_Y 
#if HORIZ_POS
													* DOWNSAMPLE_BGS_RESOLUTION_X
#endif

#else
,	FEATURE_DOWNSAMPLED_BGS_END = FEATURE_DOWNSAMPLED_END
#endif

#if USE_RUNLENGTHS
,	FEATURE_RUNLENGTH     = FEATURE_DOWNSAMPLED_BGS_END
,	FEATURE_RUNLENGTH_RL  = FEATURE_RUNLENGTH + RUNLENGTH_RESOLUTION_Y 
,	FEATURE_RUNLENGTH_END = FEATURE_RUNLENGTH_RL + RUNLENGTH_RESOLUTION_Y 
#if HORIZ_POS
												* RUNLENGTH_RESOLUTION_X
#endif
#else
,	FEATURE_RUNLENGTH_END = FEATURE_DOWNSAMPLED_BGS_END
#endif

#if USE_ENVELOPES
,	FEATURE_UPPER_ENVELOPE1     = FEATURE_RUNLENGTH_END
,	FEATURE_UPPER_ENVELOPE1_END = FEATURE_UPPER_ENVELOPE1 + ENVELOPE_RESOLUTION_X
,	FEATURE_LOWER_ENVELOPE1     = FEATURE_UPPER_ENVELOPE1_END
,	FEATURE_LOWER_ENVELOPE1_END = FEATURE_LOWER_ENVELOPE1 + ENVELOPE_RESOLUTION_X
,	FEATURE_UPPER_ENVELOPE2     = FEATURE_LOWER_ENVELOPE1_END
,	FEATURE_UPPER_ENVELOPE2_END = FEATURE_UPPER_ENVELOPE2 + ENVELOPE_RESOLUTION_X
,	FEATURE_LOWER_ENVELOPE2     = FEATURE_UPPER_ENVELOPE2_END
,	FEATURE_LOWER_ENVELOPE2_END = FEATURE_LOWER_ENVELOPE2 + ENVELOPE_RESOLUTION_X
#else
,	FEATURE_LOWER_ENVELOPE2_END = FEATURE_RUNLENGTH_END
#endif

#if USE_ANGLE_ZONES
,	FEATURE_ANGLE_ZONES     = FEATURE_LOWER_ENVELOPE2_END
,	FEATURE_ANGLE_ZONES_END = FEATURE_ANGLE_ZONES + BOUNDARY_HISTO_RESOLUTION_BASE * DOWNSAMPLE_RESOLUTION_X
#else
,	FEATURE_ANGLE_ZONES_END = FEATURE_LOWER_ENVELOPE2_END
#endif

,	NUMBER_OF_FEATURES = FEATURE_ANGLE_ZONES_END
};

// ----------------------------------------------------------------------------- : Feature vectors

typedef double Feature;

/// A vector of features. Each feature is a floating point number
class FeatureVector {
  public:
	static const size_t size = NUMBER_OF_FEATURES;
	
	/// The feature values
	//  fixed size array has decent copy semantics - but this is a large struct!
	Feature features[NUMBER_OF_FEATURES];
	
	FeatureVector() {}
	FeatureVector(Feature defaultVal);
	
	inline Feature& operator[] (size_t index)       { return features[index]; }
	inline Feature  operator[] (size_t index) const { return features[index]; }
	
	/// Fill with a default value
	void clear(Feature defaultVal);
	
	inline void operator += (const FeatureVector& that) {
		for (size_t i = 0 ; i < size ; ++i) {
			features[i] += that[i];
		}
	}
	inline void addMul(Feature c, const FeatureVector& that) {
		for (size_t i = 0 ; i < size ; ++i) {
			features[i] += c * that[i];
		}
	}
	inline void operator /= (Feature v) {
		for (size_t i = 0 ; i < size ; ++i) {
			features[i] /= v;
		}
	}
	inline void pow (Feature p) {
		for (size_t i = 0 ; i < size ; ++i) {
			features[i] = ::pow(features[i], p);
		}
	}
	inline double dot (const FeatureVector& that) const {
		double sum = 0;
		for (size_t i = 0 ; i < size ; ++i) {
			sum += features[i] * that[i];
		}
		return sum;
	}
};

// ----------------------------------------------------------------------------- : EOF
#endif
