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
	unsigned long long trainingIter;
	VectorXd values;
};

