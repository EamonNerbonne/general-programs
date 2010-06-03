#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"
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
		static LvqTrainingStatCli toCli(LvqTrainingStat cppVal) {
			LvqTrainingStatCli cliVal;
			cliVal.trainingIter = cppVal.trainingIter;
			cliVal.elapsedSeconds = cppVal.elapsedSeconds;
			cliVal.trainingError = cppVal.trainingError;
			cliVal.trainingCost = cppVal.trainingCost;
			cliVal.testError = cppVal.testError;
			cliVal.testCost = cppVal.testCost;
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
			return cppVal;
		}
	};
}
