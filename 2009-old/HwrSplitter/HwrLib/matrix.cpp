//+----------------------------------------------------------------------------+
//| Handwriting recognition - Features                                         |
//| Feature matrix type                                                        |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include "feature/matrix.h"
using namespace std;

#ifdef _MSC_VER
#pragma warning(disable:4996)
#endif

// ----------------------------------------------------------------------------- : Matrix class

Matrix::Matrix() : count(0) {}
Matrix::Matrix(const std::string& filename) : count(0) { load(filename); }

void Matrix::multiply(const FeatureVector& in, FeatureVector& out) const {
	for (unsigned int i = 0 ; i < count ; ++i) {
		out[i] = cols[i].dot(in);
	}
	for (unsigned int i = count ; i < NUMBER_OF_FEATURES ; ++i) {
		out[i] = 0; // no data
	}
}

void Matrix::multiply(FeatureVector& vec) const {
	FeatureVector new_vec;
	multiply(vec, new_vec);
	vec = new_vec;
}

void Matrix::resize(unsigned int size) {
	count = min(count, size);
}

void Matrix::disableFeature(int f) {
	for (unsigned int i = 0 ; i < count ; ++i) {
		cols[i][f] = 0.;
	}
}

// ----------------------------------------------------------------------------- : File IO

/// mathematica writes big endian files
void fread(bool le, void* buf, size_t n, size_t m, FILE* file) {
	fread(buf,n,m,file);
	if (!le) {
		for (size_t i = 0 ; i < n ; ++i) {	
			reverse((char*)buf + i*m, (char*)buf + i*m + m);
		}
	}
}

void Matrix::load(FILE* file) {
	// from: http://www.zdv.uni-tuebingen.de/static/hard/zrsinfo/matlab/R14/help/pdf_doc/matlab/matfile_format.pdf
	unsigned i;
	char name[100];
	bool le = true;
	fread(le, &i, 1, 4, file); // little endian?, double, dense
	if (i == 0)          le = true;
	if (i == 0xE8030000) le = false; // = 1000 in be
	else throw "Unsupported format"; 
	fread(le, &i, 1, 4, file); if (i != NUMBER_OF_FEATURES) throw "wrong number of rows"; // rows
	fread(le, &i, 1, 4, file); count = i; if (count > NUMBER_OF_FEATURES) throw "too many cols"; // cols
	fread(le, &i, 1, 4, file); if (i != 0) throw "Unsupported format"; // real
	fread(le, &i, 1, 4, file); // length(name\0)
	fread(le, name, i, 1, file); // name including terminating 0
	// read columns
	for (unsigned int i = 0 ; i < count ; ++i) {
		fread(le, cols[i].features, NUMBER_OF_FEATURES, sizeof(double), file);
	}
}

void Matrix::load(const std::string& filename) {
	FILE* file = fopen(filename.c_str(), "rb");
	if (!file) {
		throw "Database file not found!";
	}
	load(file);
	fclose(file);
}
