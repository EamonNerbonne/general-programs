// This is the main DLL file.

#include "stdafx.h"

#include "HwrOptimizer.h"

#include "image/transformations.h"

namespace HwrLibCliWrapper {



	void HwrOptimizer::SplitWords(ImageStruct<signed char> block, int cropXoffset, HwrDataModel::TextLine^ textLine, SymbolLearningData ^ learningCache  ) {
		using std::min;
		using std::max;
		using std::cout;
		using std::abs;
#if LOGLEVEL >=8
		boost::timer t;
#endif
		int learningIteration = managedSymbols->Iteration;
		//based on learningIteration, set a few things:
		double dampingFactor = 1.0 - min(learningIteration/200.0,1.0);
		int blurIter = 3;
		int winAngleSize = int(100.0*dampingFactor + 4);
		int winDensSize = int(winAngleSize*0.76);
		double featureRelevance = FEATURE_SCALING * exp(-20*dampingFactor) ;

		PamImage<BWPixel> shearedImg = ImageProcessor::StructToPamImage(block);
		ImageBW deshearedImg = processAndUnshear(shearedImg, (float)textLine->shear, textLine->bodyTop,textLine->bodyBot);//bodyTop/bodyBot are relative to line top, not to page top.
		int topShearOffset = shearedImg.getWidth() - deshearedImg.getWidth();

		ImageFeatures feats(deshearedImg,textLine->bodyTop,textLine->bodyBot, winDensSize,winAngleSize,blurIter);
		textLine->bodyBot = feats.baseline; //these should not have changed.
		textLine->bodyTop = feats.topline; //these should not have changed.

#ifdef _DEBUG
		int shearedW = shearedImg.getWidth();
		int shearedH = shearedImg.getHeight();
		int unshearedW = deshearedImg.getWidth();
		int unshearedH = deshearedImg.getHeight();
#endif

		array<wchar_t>^ textArray = Enumerable::ToArray(textLine->TextWithTerminators);
		array<int> ^ manualEndsArray = Enumerable::ToArray(textLine->ManualEndPoints);
		vector<short> symbolCodeVector;
		vector<int> manualEndsVector;
		for(int i=0;i<textArray->Length;i++) {
			unsigned charCode = managedSymbols->LookupSymbolCode(textArray[i]);
			int manualEndPoint = manualEndsArray[i]>=0  ?  manualEndsArray[i] - cropXoffset - topShearOffset  :  -1;
			if(manualEndPoint >shearedImg.getWidth()) {
				Console::WriteLine("#");
				manualEndPoint = -1; 
			}
			symbolCodeVector.push_back((short)charCode);
			manualEndsVector.push_back(manualEndPoint);
		}
		if(!(
			textArray->Length == manualEndsArray->Length
			&& textArray->Length == symbolCodeVector.size()
			&& textArray->Length == manualEndsVector.size())) {
				throw gcnew ApplicationException(L"Error: sequences are not of equal length"
#if 0
					+L"; text:"+textArray->Length.ToString() 
					+ L", manualEnds:"+manualEndsArray->Length.ToString()
					+L", symbolCodeV: "+symbolCodeVector.size().ToString()
					+L", manualEndsV:"+  manualEndsVector.size().ToString()
#endif
					);
		}


#if LOGLEVEL >=8
		cout << "C++ textline prepare took " << t.elapsed() <<"\n";
#endif
		WordSplitSolver splitSolve(*nativeSymbols->GetSymbols(), feats, symbolCodeVector, manualEndsVector, featureRelevance); //computes various prob. distributions
		
		double computedLikelihood;
		vector<int> splits = splitSolve.MostLikelySplit(computedLikelihood);//these, of course, are computed relative to the sheared image, i.e. you need to add topShearOffset + cropXoffset for absolute coordinates.
		
		
		array<int>^ absoluteEndpoints = gcnew array<int>((int)splits.size());
		for(int i=0;i<(int)splits.size();i++) 
			absoluteEndpoints[i] = splits[i] + topShearOffset + cropXoffset;

		textLine->SetComputedCharEndpoints(absoluteEndpoints, computedLikelihood, HwrDataModel::Word::TrackStatus::Calculated);
		if(learningCache->GetSymbols()->CheckConsistency() > 0)
			throw gcnew ApplicationException("NaN's found in learning cache: "+textLine->FullText);
		splitSolve.Learn(dampingFactor, *learningCache->GetSymbols());
		if(learningCache->GetSymbols()->CheckConsistency() > 0)
			throw gcnew ApplicationException("NaN's found in learning cache after learning: "+textLine->FullText );
	}
}