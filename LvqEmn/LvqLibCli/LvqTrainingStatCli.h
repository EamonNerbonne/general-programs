#pragma once
#include "stdafx.h"

using namespace System;

namespace LvqLibCli {
	public value class LvqTrainingStatCli {
	public:
		literal int ElapsedSecondsStat = LvqTrainingStats::ElapsedSeconds;
		literal int TrainingErrorStat = LvqTrainingStats::TrainingError;
		literal int TrainingCostStat = LvqTrainingStats::TrainingCost;
		literal int TestErrorStat = LvqTrainingStats::TestError;
		literal int TestCostStat = LvqTrainingStats::TestCost;
		literal int PNormStat = LvqTrainingStats::PNorm;
		literal int ExtraStat = LvqTrainingStats::Extra;
		
		int trainingIter;
		array<double>^ values;
		array<double>^ stderror;

		
		static LvqTrainingStatCli toCli(LvqTrainingStat cppVal) {
			LvqTrainingStatCli cliVal;
			cliVal.trainingIter = cppVal.trainingIter;
			cppToCli(cppVal.values, cliVal.values);
			return cliVal;
		}

		static LvqTrainingStatCli toCli(int trainingIter, Eigen::VectorXd const &  values, Eigen::VectorXd const & stderror) {
			LvqTrainingStatCli cliVal;
			cliVal.trainingIter = trainingIter;
			cppToCli(values, cliVal.values);
			cppToCli(stderror,cliVal.stderror);
			return cliVal;
		}

		static LvqTrainingStat toCpp(LvqTrainingStatCli cliVal) {
			LvqTrainingStat cppVal;
			cppVal.trainingIter = cliVal.trainingIter;
			cliToCpp(cliVal.values,cppVal.values);
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
