#include "StdAfx.h"
#include "SymbolLearningData.h"

namespace HwrLibCliWrapper {
	SymbolLearningData::!SymbolLearningData() {
		if(symbols != NULL) {
			GC::RemoveMemoryPressure(symbols->AllocatedSize());
			delete symbols;
			symbols = NULL;
		}
	}
	SymbolLearningData::~SymbolLearningData() { this->!SymbolLearningData(); }
	SymbolLearningData::SymbolLearningData(int symbolCount) 
		: symbols(new AllSymbolClasses(symbolCount ))
	{
		GC::AddMemoryPressure(symbols->AllocatedSize());
	}

	void SymbolLearningData::CopyToNative(HwrDataModel::SymbolClass ^ managedSymbol, SymbolClass & nativeSymbol) {
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

	void SymbolLearningData::CopyToManaged(SymbolClass const & nativeSymbol, HwrDataModel::SymbolClass^ managedSymbol){
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

	void SymbolLearningData::SaveToManaged(HwrDataModel::SymbolClasses^ managedSymbols) {
		if(managedSymbols->Count != symbols->size())
			throw gcnew ArgumentException("Manage symbol set is not of equal size to native symbol set");

		for(int i=0;i<managedSymbols->Count;i++) {
			if(managedSymbols[i]->Code != i)
				throw gcnew ArgumentException("Symbol position does not match its code");
			CopyToManaged(symbols->getSymbol(i), managedSymbols[i]);
		}
		managedSymbols->Iteration = symbols->iteration;
	}

	void SymbolLearningData::LoadFromManaged(HwrDataModel::SymbolClasses^ managedSymbols) {
			for(int i=0;i<managedSymbols->Count;i++) {
				if(managedSymbols[i]->Code != i)
					throw gcnew ArgumentException("Symbol position does not match its code");
				CopyToNative(managedSymbols[i], symbols->getSymbol(i));
			}
			symbols->iteration = managedSymbols->Iteration;
	}

	void SymbolLearningData::MergeInLearningCache(SymbolLearningData^ learningData) {
		if(symbols->CheckConsistency() > 0)
			throw gcnew ApplicationException("My symbols inconsistent");
		if(learningData->symbols->CheckConsistency() > 0)
			throw gcnew ApplicationException("learning symbols inconsistent");
		symbols->CombineWithDistributions(*learningData->symbols);
		learningData->Reset();
		if(symbols->CheckConsistency() > 0)
			throw gcnew ApplicationException("learning result symbols inconsistent");
	}
	void SymbolLearningData::AssertConsistency(System::String^ message) {
		if(symbols->CheckConsistency() > 0)
			throw gcnew ApplicationException("Symbols inconsistent:"+message);
	}
}