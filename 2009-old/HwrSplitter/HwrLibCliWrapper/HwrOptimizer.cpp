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


	array<int>^ HwrOptimizer::SplitWords(ImageStruct<signed char> block, float shear, array<unsigned> ^ sequenceToMatch, array<int> ^ overrideEnds,  int learningIteration, HwrDataModel::TextLine^ textLine, [Out] int % topOffRef, [Out] double % loglikelihood) {
		using std::min;
		using std::max;
		using std::cout;
		using std::abs;
#if LOGLEVEL >=8
		boost::timer t;
#endif
		//based on learningIteration, set a few things:
		double dampingFactor = 1.0 - min(learningIteration/200.0,1.0);
		int blurIter = 3;
		int winAngleSize = int(100.0*dampingFactor + 4);
		int winDensSize = int(winAngleSize*0.76);
		double featureRelevance = FEATURE_SCALING * exp(-20*dampingFactor) ;

		PamImage<BWPixel> shearedImg = ImageProcessor::StructToPamImage(block);
		ImageBW unsheared = processAndUnshear(shearedImg,shear,textLine->bodyTop,textLine->bodyBot);
		topOffRef = shearedImg.getWidth() - unsheared.getWidth();
		ImageFeatures feats(unsheared,textLine->bodyTop,textLine->bodyBot, winDensSize,winAngleSize,blurIter);
		textLine->bodyBot = feats.baseline;
		textLine->bodyTop = feats.topline;

#ifdef _DEBUG
		int shearedW = shearedImg.getWidth();
		int shearedH = shearedImg.getHeight();
		int unshearedW = unsheared.getWidth();
		int unshearedH = unsheared.getHeight();
#endif

		if(sequenceToMatch->Length != overrideEnds->Length)
			throw gcnew ArgumentException("overrideEnds must be equally long as sequenceToMatch");
		vector<short> sequenceVector;
		vector<int> overrideEndsVector;
		for(int i=0;i<sequenceToMatch->Length;i++) {
			unsigned tmp = sequenceToMatch[i];
			sequenceVector.push_back(tmp);
			int endPoint = overrideEnds[i]-topOffRef;
			overrideEndsVector.push_back(endPoint);
		}


#if LOGLEVEL >=8
		cout << "C++ textline prepare took " << t.elapsed() <<"\n";
#endif
		WordSplitSolver splitSolve( *symbols, feats, sequenceVector,overrideEndsVector,featureRelevance);
		
		double computedLikelihood;
		vector<int> splits = splitSolve.MostLikelySplit(computedLikelihood);
		loglikelihood = computedLikelihood;
		array<int>^ retval = gcnew array<int>((int)splits.size());
		for(int i=0;i<(int)splits.size();i++) {
			retval[i] = splits[i];
		}

		splitSolve.Learn(dampingFactor);

		return retval;
	}
}