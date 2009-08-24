// This is the main DLL file.

#include "stdafx.h"

#include "HwrOptimizer.h"

#include "image/transformations.h"

namespace HwrLibCliWrapper {

	array<int>^ HwrOptimizer::SplitWords(ImageStruct<signed char> block, array<unsigned> ^ sequenceToMatch, [Out] int % topOffRef,float shear) {
		using std::min;
		using std::max;
		using std::cout;
		boost::timer t;

		PamImage<BWPixel> shearedImg = ImageProcessor::StructToPamImage(block);
		ImageBW unsheared = unshear(shearedImg,shear);
		topOffRef = shearedImg.getWidth() - unsheared.getWidth();
		ImageFeatures feats(unsheared);
		vector<short> sequenceVector;
		for(int i=0;i<sequenceToMatch->Length;i++) {
			unsigned tmp = sequenceToMatch[i];
			sequenceVector.push_back(tmp);
		}

		cout << "C++ textline prepare took " << t.elapsed() <<"\n";
		WordSplitSolver splitSolve( *symbols, feats, sequenceVector);

		vector<int> splits = splitSolve.MostLikelySplit();
		array<int>^ retval = gcnew array<int>((int)splits.size());
		for(int i=0;i<(int)splits.size();i++) {
			retval[i] = splits[i];
		}

		splitSolve.Learn();

		return retval;
	}
}