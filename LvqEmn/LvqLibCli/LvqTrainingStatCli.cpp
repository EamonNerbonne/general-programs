#include "StdAfx.h"
#include "LvqTrainingStatCli.h"

namespace LvqLibCli {
	LvqTrainingStatCli LvqTrainingStatCli::toCli(Eigen::VectorXd const & cppVal) {
		LvqTrainingStatCli cliVal;
		cppToCli(cppVal, cliVal.values);
		return cliVal;
	}

	LvqTrainingStatCli LvqTrainingStatCli::toCli(Eigen::VectorXd const &  values, Eigen::VectorXd const & stderror) {
		LvqTrainingStatCli cliVal;
		cppToCli(values, cliVal.values);
		cppToCli(stderror,cliVal.stderror);
		return cliVal;
	}

	VectorXd LvqTrainingStatCli::toCpp(LvqTrainingStatCli cliVal) { //TODO:unneeded, remove?
		VectorXd cppVal;
		cliToCpp(cliVal.values,cppVal);
		return cppVal;
	}

	void cliToCpp(LvqTrainingStatCli % stat, VectorXd &retval) {
		retval = LvqTrainingStatCli::toCpp(stat);
	}

	void cppToCli(VectorXd const & stat, LvqTrainingStatCli% retval) {
		retval = LvqTrainingStatCli::toCli(stat);
	}
}
