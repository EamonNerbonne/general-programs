// This is the main DLL file.

#include "stdafx.h"

#include "HwrOptimizer.h"

#include "image/transformations.h"

namespace HwrLibCliWrapper {

	void HwrOptimizer::CopyToNative(HwrDataModel::SymbolClass ^ managedSymbol, SymbolClass & nativeSymbol) {
		nativeSymbol.wLength = managedSymbol->Length->WeightSum;//1.0;//
		nativeSymbol.mLength = managedSymbol->Length->Mean;
		nativeSymbol.sLength = managedSymbol->Length->ScaledVariance;///managedSymbol->Length->WeightSum;
		nativeSymbol.originalChar = managedSymbol->Letter;
		if(managedSymbol->SubPhase!=nullptr && managedSymbol->SubPhase->Length == SUB_PHASE_COUNT) { //OK, we can deal with that...
			for(int i=0;i<SUB_PHASE_COUNT;i++) {
				if(managedSymbol->SubPhase[i] !=nullptr && managedSymbol->SubPhase[i]->Length > 0) {//OK, the sub-states look OK...
					for(int j=0; j < SUB_STATE_COUNT; j++) {//foreach native substate
						HwrDataModel::FeatureDistributionEstimate ^ mState = managedSymbol->SubPhase[i][j% managedSymbol->SubPhase[i]->Length]; //it's ok to have different numbers of states.
						if(mState != nullptr 
								&& mState->means != nullptr 
								&& mState->means->Length == NUMBER_OF_FEATURES
								&& mState->scaledVars != nullptr 
								&& mState->scaledVars->Length == NUMBER_OF_FEATURES) {//looks like a valid featuredistribution!
							FeatureDistribution & state = nativeSymbol.phase[i].state[j];
							state.weightSum = mState->weightSum;// 1.0;//
							for(int j=0;j<NUMBER_OF_FEATURES;j++) {
								state.meanX[j] = mState->means[j];
								state.sX[j] = mState->scaledVars[j]; /// mState->weightSum;
							}
							state.RecomputeDCfactor();
						}//endif-valid-featuredistribution
					}//end-foreach native substate
				}//end-if substates OK
			}//end-foreach phase
		}//end-if subphases OK
	}

	void HwrOptimizer::CopyToManaged(SymbolClass const & nativeSymbol, HwrDataModel::SymbolClass^ managedSymbol){
		using namespace HwrDataModel;
		if(managedSymbol->Letter != nativeSymbol.originalChar)
			throw gcnew ApplicationException("characters in C++ and C# out of sync");
		managedSymbol->Length = gcnew GaussianEstimate(nativeSymbol.mLength,nativeSymbol.sLength,nativeSymbol.wLength);
		managedSymbol->SubPhase = gcnew array<array<FeatureDistributionEstimate^>^>(SUB_PHASE_COUNT);
		for(int i=0;i<SUB_PHASE_COUNT;i++) {
			managedSymbol->SubPhase[i] = gcnew array<FeatureDistributionEstimate^>(SUB_STATE_COUNT);
			for(int j=0; j < SUB_STATE_COUNT; j++) {
				FeatureDistributionEstimate ^ mState = gcnew FeatureDistributionEstimate();
				managedSymbol->SubPhase[i][j] = mState;
				FeatureDistribution const & state = nativeSymbol.phase[i].state[j];
				mState->weightSum = state.weightSum;
				mState->means = gcnew array<double>(NUMBER_OF_FEATURES);
				mState->scaledVars = gcnew array<double>(NUMBER_OF_FEATURES);

				for(int k=0;k<NUMBER_OF_FEATURES;k++) {
					mState->means[k] = state.meanX[k];
					mState->scaledVars[k]=state.sX[k];
				}
			}
		}
	}


	void HwrOptimizer::SplitWords(ImageStruct<signed char> block, int cropXoffset, HwrDataModel::TextLine^ textLine, SymbolLearningData ^ dataSink  ) {
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
		ImageBW unsheared = processAndUnshear(shearedImg, (float)textLine->shear, textLine->bodyTop,textLine->bodyBot);//bodyTop/bodyBot are relative to line top, not to page top.
		int topShearOffset = unsheared.getWidth() - shearedImg.getWidth();

		ImageFeatures feats(unsheared,textLine->bodyTop,textLine->bodyBot, winDensSize,winAngleSize,blurIter);
		textLine->bodyBot = feats.baseline; //these should not have changed.
		textLine->bodyTop = feats.topline; //these should not have changed.

#ifdef _DEBUG
		int shearedW = shearedImg.getWidth();
		int shearedH = shearedImg.getHeight();
		int unshearedW = unsheared.getWidth();
		int unshearedH = unsheared.getHeight();
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
			&& textArray->Length == manualEndsVector.size()))
			throw gcnew ApplicationException(
									String::Format("Error: sequences are not of equal length; text:{0}, manualEnds:{1}, symbolCodeV:{2}, manualEndsV:{3}", gcnew array<Object^>{textArray->Length, manualEndsArray->Length, symbolCodeVector.size(), manualEndsVector.size()} )
								);


#if LOGLEVEL >=8
		cout << "C++ textline prepare took " << t.elapsed() <<"\n";
#endif
		WordSplitSolver splitSolve(*symbols, feats, symbolCodeVector, manualEndsVector, featureRelevance); //computes various prob. distributions
		
		double computedLikelihood;
		vector<int> splits = splitSolve.MostLikelySplit(computedLikelihood);//these, of course, are computed relative to the sheared image, i.e. you need to add topShearOffset + cropXoffset for absolute coordinates.
		
		
		array<int>^ absoluteEndpoints = gcnew array<int>((int)splits.size());
		for(int i=0;i<(int)splits.size();i++) 
			absoluteEndpoints[i] = splits[i] + topShearOffset + cropXoffset;

		textLine->SetComputedCharEndpoints(absoluteEndpoints, computedLikelihood, HwrDataModel::Word::TrackStatus::Calculated);

		splitSolve.Learn(dampingFactor, *dataSink->DataSink()); //TODO:fix up
		dataSink->IncIteration();
	}
}