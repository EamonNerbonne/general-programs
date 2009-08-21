//+----------------------------------------------------------------------------+
//| Handwriting recognition - Features                                         |
//| Feature matrix type                                                        |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#ifndef MATRIX_H
#define MATRIX_H

// ----------------------------------------------------------------------------- : Includes

#include "../HwrConfig.h"
#include "featurevector.h"


// ----------------------------------------------------------------------------- : Matrix class

class Matrix {
  public:
	Matrix();
	Matrix(const std::string& filename);
	/// Multiply the matrix with a vector, store result in out
	/** Can not be used in place */
	void multiply(const FeatureVector& in, FeatureVector& out) const;
	/// Multiply the matrix with a vector in place
	void multiply(FeatureVector& vec) const;
	
	/// drop some rows
	void resize(unsigned int size);
	unsigned int size() const { return count; };
	// zero out some columns
	void disableFeature(int f);
	
	/// Load a .MAT file
	void load(FILE* file);
	void load(const std::string& filename);
  private:
	FeatureVector cols[NUMBER_OF_FEATURES];
	unsigned int count;
};

// ----------------------------------------------------------------------------- : EOF
#endif
