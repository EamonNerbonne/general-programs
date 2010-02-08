BOOSTINC="C:\Program Files (Custom)\boost_1_41_0"
CFLAGS= -I $(BOOSTINC) -march=native -mtune=native -O3 -DEIGEN_DONT_VECTORIZE -DNDEBUG

EIGEN2INC="C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen2"
V2FLAGS = -DEIGEN2 -I $(EIGEN2INC) $(CFLAGS)

EIGEN3INC="C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen"
V3FLAGS = -DEIGEN3 -I $(EIGEN3INC) $(CFLAGS)


OBJDIR_V2=obj_v2
OBJDIR_V3=obj_v3

SOURCES = stdafx.cpp BoostMatrixTest.cpp DataSetUtils.cpp G2mLvqModel.cpp GmLvqModel.cpp GsmLvqModel.cpp LvqDataSet.cpp utils.cpp
DEPS = AbstractLvqModel.h BoostMatrixTest.h DataSetUtils.h G2mLvqMatch.h G2mLvqModel.h G2mLvqPrototype.h GmLvqModel.h GsmLvqModel.h LvqConstants.h LvqDataSet.h stdafx.h utils.h 
OBJECTS = $(SOURCES:.cpp=.o)
OBJ_V2 = $(patsubst %,$(OBJDIR_V2)/%,$(OBJECTS))
OBJ_V3 = $(patsubst %,$(OBJDIR_V3)/%,$(OBJECTS))

$(OBJDIR_V2)/%.o: %.cpp $(DEPS)
	g++ -c -o $@ $<  $(V2FLAGS)
	
$(OBJDIR_V3)/%.o: %.cpp $(DEPS)
	g++ -c -o $@ $< $(V3FLAGS)

all:  liblvq_v3.a liblvq_v2.a

liblvq_v2.a: $(OBJ_V2)
	ar rcs $@ $^

liblvq_v3.a: $(OBJ_V3)
	ar rcs $@ $^
	
.PHONY: clean

clean:
	del "$(OBJDIR_V3)\*.o" "$(OBJDIR_V2)\*.o" liblvq_v2.a liblvq_v3.a /Q
	
#@ECHO OFF
#REM g++ -I "C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen" -O2 -msse2 -DNDEBUG   -o benchDev DoTest.cpp learningBench.cpp main.cpp mulBench.cpp 
#REM -floop-interchange -floop-strip-mine -floop-block

#g++ -I "C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen" -march=native -mtune=native -O3 -DEIGEN_DONT_VECTORIZE -DEIGEN3 -DNDEBUG -o benchDev DoTest.cpp learningBench.cpp main.cpp mulBench.cpp 

#REM -floop-interchange -floop-strip-mine -floop-block