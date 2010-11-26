#include "StdAfx.h"
#include "LvqModelSettings.h"
#include "LvqModel.h"

#include "GmmLvqModel.h"
#include "G2mLvqModel.h"
#include "GmLvqModel.h"
#include "GsmLvqModel.h"

LvqModel* ConstructLvqModel(LvqModelSettings & initSettings) {
	switch(initSettings.ModelType) {
	case LvqModelSettings::GmModelType:
		return new GmLvqModel(initSettings);
		break;
	case LvqModelSettings::GsmModelType:
		return new GsmLvqModel(initSettings);
		break;
	case LvqModelSettings::G2mModelType:
		return new G2mLvqModel(initSettings);
		break;
	case LvqModelSettings::GmmModelType:
		return new GmmLvqModel(initSettings);
		break;
	default:
		return 0;
		break;
	}
}