#pragma once
#include "stdafx.h"

using namespace System;

namespace LvqLibCli {
	public value class LvqTrainingStatCli {
	public:
		literal int TrainingIterationI = 0;
		literal int ElapsedSecondsI = 1;
		literal int TrainingErrorI = 2;
		literal int TrainingCostI = 3;
		literal int TestErrorI = 4;
		literal int TestCostI = 5;
		literal int ExtraI = 5;

		array<double>^ values;
		array<double>^ stderror;
		
		static LvqTrainingStatCli toCli(Eigen::VectorXd const & cppVal);
		static LvqTrainingStatCli toCli(Eigen::VectorXd const &  values, Eigen::VectorXd const & stderror);
		static VectorXd toCpp(LvqTrainingStatCli cliVal);
	};

	void cliToCpp(LvqTrainingStatCli % stat, VectorXd &retval);
	void cppToCli(VectorXd const & stat, LvqTrainingStatCli% retval);
}
