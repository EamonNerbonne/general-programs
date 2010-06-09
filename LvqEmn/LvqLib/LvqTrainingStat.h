#pragma once
#include "stdafx.h"
#include "utils.h"

namespace LvqTrainingStats {
enum LvqTrainingStatsEnum {
	 ElapsedSeconds,
	 TrainingError,
	 TrainingCost,
	 TestError,
	 TestCost,
	 PNorm,
	 Extra
}; };

struct LvqTrainingStat {
	int trainingIter;
	VectorXd values;
};

