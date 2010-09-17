#include "StdAfx.h"
#include "LvqModelSettings.h"
#include "LvqModel.h"

#include "G2mLvqModel.h"
#include "GmLvqModel.h"
#include "GsmLvqModel.h"

LvqModel* ConstructLvqModel(LvqModelInitSettings & initSettings) {
	switch(initSettings.ModelType) {
	case LvqModelInitSettings::GmModelType:
		return new GmLvqModel(initSettings);
		break;
	case LvqModelInitSettings::GsmModelType:
		return new GsmLvqModel(initSettings);
		break;
	case LvqModelInitSettings::G2mModelType:
		return new G2mLvqModel(initSettings);
		break;
	default:
		return 0;
		break;
	}
}