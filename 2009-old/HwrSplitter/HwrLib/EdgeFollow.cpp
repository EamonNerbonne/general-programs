#include "stdafx.h"
#include "EdgeFollow.h"

using std::vector;

Point add(Point a, Point b) {return Point(a.X+b.X,a.Y+b.Y);}
EdgeDirection next(EdgeDirection dir) {return EdgeDirection((dir+1)%4);}
EdgeDirection prev(EdgeDirection dir) {return EdgeDirection((dir+3)%4);}

static const Point Offset [] = { Point(0,-1), Point(1,0), Point(0,1), Point(-1,0) };


bool EdgeComponent::IsValidIn(PamImage<BWPixel> const & image) {	return P.isSet(image) && !add(P,Offset[Side]).isSet(image);}

EdgeComponent ClockwiseStep(PamImage<BWPixel> const & image, EdgeComponent current) {
	assert(current.IsValidIn(image));
	Point straightIfSet = add(current.P, Offset[next(current.Side)]);
	Point leftTurnIfSet = add(straightIfSet,Offset[current.Side]);
	return 
		leftTurnIfSet.isSet(image) ? EdgeComponent(leftTurnIfSet, prev(current.Side))
		: straightIfSet.isSet(image) ? EdgeComponent(straightIfSet, current.Side)
		: EdgeComponent(current.P, next(current.Side));
}

EdgeComponent CounterClockwiseStep(PamImage<BWPixel> const & image, EdgeComponent current) {
	assert(current.IsValidIn(image));
	Point straightIfSet = add(current.P, Offset[prev(current.Side)]);
	Point rightTurnIfSet = add(straightIfSet,Offset[current.Side]);
	return 
		rightTurnIfSet.isSet(image) ? EdgeComponent(rightTurnIfSet, next(current.Side))
		: straightIfSet.isSet(image) ? EdgeComponent(straightIfSet, current.Side)
		: EdgeComponent(current.P, prev(current.Side));
}

EdgeComponent ClockwiseSteps(PamImage<BWPixel> const & image, EdgeComponent current, int n) {
	for(int i=0;i<n;++i) 	current = ClockwiseStep(image,current);
	return current;
}
EdgeComponent CounterClockwiseSteps(PamImage<BWPixel> const & image, EdgeComponent current, int n) {
	for(int i=0;i<n;++i) 	current = CounterClockwiseStep(image,current);
	return current;
}

vector<EdgeComponent> ClockwiseEdge(PamImage<BWPixel> const & image, EdgeComponent initial) {
	vector<EdgeComponent> edge;
	edge.push_back(initial);
	for (EdgeComponent current = ClockwiseStep(image, initial); current != initial; current = ClockwiseStep(image, current))
		edge.push_back(current);
	return edge;
}

vector<EdgeComponent> CounterClockwiseEdge(PamImage<BWPixel> const & image, EdgeComponent initial) {
	vector<EdgeComponent> edge;
	edge.push_back(initial);
	for (EdgeComponent current = CounterClockwiseStep(image, initial); current != initial; current = CounterClockwiseStep(image, current))
		edge.push_back(current);
	return edge;
}