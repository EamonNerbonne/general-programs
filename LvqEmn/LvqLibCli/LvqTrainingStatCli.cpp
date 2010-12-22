#include "StdAfx.h"
#include "LvqTrainingStatCli.h"

namespace LvqLibCli {
	void cliToCpp(LvqTrainingStatCli % cliVal, VectorXd &cppVal) {
		cliToCpp(cliVal.values,cppVal);
	}

	void cppToCli(VectorXd const & stat, LvqTrainingStatCli% retval) {
		cppToCli(stat, retval.values);
	}
	void cppToCli(std::vector<double> const  & stat, LvqTrainingStatCli% retval) {
		cppToCli(stat, retval.values);
	}
}
