#pragma once
#include "stdafx.h"
#include "WrappingUtils.h"

using namespace System;

namespace LvqLibCli {
	public value class LvqTrainingStatCli {
	public:
		int trainingIter;
		double elapsedSeconds;
		double trainingError;
		double trainingCost;
		double testError;
		double testCost;
		double pNorm;
		array<double>^ otherStats;

		static LvqTrainingStatCli toCli(LvqTrainingStat cppVal) {
			LvqTrainingStatCli cliVal;
			cliVal.trainingIter = cppVal.trainingIter;
			cliVal.elapsedSeconds = cppVal.elapsedSeconds;
			cliVal.trainingError = cppVal.trainingError;
			cliVal.trainingCost = cppVal.trainingCost;
			cliVal.testError = cppVal.testError;
			cliVal.testCost = cppVal.testCost;
			cliVal.pNorm = cppVal.pNorm;
			cliVal.otherStats = cppToCli(cppVal.otherStats);
			return cliVal;
		}

		static LvqTrainingStat toCpp(LvqTrainingStatCli cliVal) {
			LvqTrainingStat cppVal;
			cppVal.trainingIter = cliVal.trainingIter;
			cppVal.elapsedSeconds = cliVal.elapsedSeconds;
			cppVal.trainingError = cliVal.trainingError;
			cppVal.trainingCost = cliVal.trainingCost;
			cppVal.testError = cliVal.testError;
			cppVal.testCost = cliVal.testCost;

			cppVal.pNorm = cliVal.pNorm;
			cppVal.otherStats = cliToCpp(cliVal.otherStats);
			return cppVal;
		}
	};
	inline void cliToCpp(LvqTrainingStatCli % stat,LvqTrainingStat &retval) {
		retval = LvqTrainingStatCli::toCpp(stat);
	}

//	template<typename T> 
	inline void cppToCli(LvqTrainingStat const & stat,LvqTrainingStatCli% retval) {
		retval = LvqTrainingStatCli::toCli(stat);
	}

}
