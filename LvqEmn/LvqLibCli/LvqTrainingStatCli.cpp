#include "StdAfx.h"
#include "LvqTrainingStatCli.h"

namespace LvqLibCli {
    void cliToCpp(LvqTrainingStatCli% cliVal, Vector_Stat& cppVal) {
        cliToCpp(cliVal.values, cppVal);
    }

    void cppToCli(Vector_Stat const& stat, LvqTrainingStatCli% retval) {
        cppToCli(stat, retval.values);
    }
    void cppToCli(std::vector<LvqStat> const& stat, LvqTrainingStatCli% retval) {
        cppToCli(stat, retval.values);
    }
}
