#include "StdAfx.h"
#include "LvqTrainingStatCli.h"

namespace LvqLibCli {
	LvqTrainingStatCli toCli(Eigen::VectorXd const & cppVal) {
		LvqTrainingStatCli cliVal;
		cppToCli(cppVal, cliVal.values);
		return cliVal;
	}

	VectorXd toCpp(LvqTrainingStatCli cliVal) { //TODO:unneeded, remove?
		VectorXd cppVal;
		cliToCpp(cliVal.values,cppVal);
		return cppVal;
	}

	void cliToCpp(LvqTrainingStatCli % stat, VectorXd &retval) {
		retval = toCpp(stat);
	}

	void cppToCli(VectorXd const & stat, LvqTrainingStatCli% retval) {
		retval = toCli(stat);
	}
}
