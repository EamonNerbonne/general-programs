#pragma once
#include "LvqTypedefs.h"
namespace LvqLibCli {
    using namespace System;
    public value class LvqTrainingStatCli {
    public:
        literal int TrainingIterationI = 0;
        literal int ElapsedSecondsI = 1;
        literal int TrainingErrorI = 2;
        literal int TrainingCostI = 3;
        literal int TestErrorI = 4;
        literal int TestCostI = 5;
        literal int ProtoJDistI = 6;
        literal int ProtoKDistI = 7;
        literal int ProtoJDistVarI = 8;
        literal int ProtoKDistVarI = 9;
        literal int CumLearningRateI = 10;
        literal int ExtraI = 11;

        array<LvqStat>^ values;
        
    };

    void cliToCpp(LvqTrainingStatCli % stat, Vector_Stat &retval);
    void cppToCli(Vector_Stat const & stat, LvqTrainingStatCli% retval);
    void cppToCli(std::vector<LvqStat> const & stat, LvqTrainingStatCli% retval);
}
