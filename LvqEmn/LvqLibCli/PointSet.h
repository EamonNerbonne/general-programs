#pragma once
namespace LvqLibCli {
	using namespace System::Windows;

	public value class CliLvqLabelledPoint {
	public:
		Point point;
		int label;
		CliLvqLabelledPoint(Point point, int label) :point(point),label(label){}
	};
	public value class ModelProjection {
	public:
		initonly array<CliLvqLabelledPoint>^   Points;
		initonly array<CliLvqLabelledPoint>^ Prototypes;
		ModelProjection(array<CliLvqLabelledPoint>^ points,array<CliLvqLabelledPoint>^ prototypes) :Points(points),Prototypes(prototypes) {}
		property bool HasValue {bool get(){return Points!=nullptr && Prototypes!=nullptr;}}
	};
}