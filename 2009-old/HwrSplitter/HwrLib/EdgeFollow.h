#pragma once
#include "image/pamImage.h"
#include <vector>

struct Point { 
	int X, Y; 
	Point(int x,int y) : X(x), Y(y) {}
	bool isSet(PamImage<BWPixel> const & image) {return X>=0  &&  Y>=0  &&  X<image.getWidth()  &&  Y<image.getHeight()  &&  image.pix(X,Y);}
};

inline bool operator ==(Point a, Point b) { return a.X==b.X && a.Y==b.Y;}
inline bool operator !=(Point a, Point b) { return a.X != b.X || a.Y != b.Y;}
inline bool operator <(Point a, Point b) { return  a.Y<b.Y || a.Y==b.Y && a.X < b.X;}
inline bool operator >(Point a, Point b) { return  a.Y>b.Y || a.Y==b.Y && a.X > b.X;}

enum EdgeDirection { TopEdge, RightEdge, BottomEdge, LeftEdge };

struct EdgeComponent {
	Point P; 
	EdgeDirection Side; 
	EdgeComponent(Point p, EdgeDirection side) : P(p), Side(side) {} 
	bool IsValidIn(PamImage<BWPixel> const & image);
};
inline bool operator == (EdgeComponent a, EdgeComponent b) {return a.P==b.P && a.Side==b.Side;}
inline bool operator != (EdgeComponent a, EdgeComponent b) {return a.P != b.P || a.Side != b.Side;}
inline bool operator <(EdgeComponent a, EdgeComponent b) { return  a.P<b.P || a.P==b.P && a.Side < b.Side;}
inline bool operator >(EdgeComponent a, EdgeComponent b) { return  a.P>b.P || a.P==b.P && a.Side > b.Side;}


EdgeComponent ClockwiseStep(PamImage<BWPixel> const & image, EdgeComponent current);
EdgeComponent ClockwiseSteps(PamImage<BWPixel> const & image, EdgeComponent current,int n);
std::vector<EdgeComponent> ClockwiseEdge(PamImage<BWPixel> const & image, EdgeComponent initial);
EdgeComponent CounterClockwiseStep(PamImage<BWPixel> const & image, EdgeComponent current);
EdgeComponent CounterClockwiseSteps(PamImage<BWPixel> const & image, EdgeComponent current,int n);
std::vector<EdgeComponent> CounterClockwiseEdge(PamImage<BWPixel> const & image, EdgeComponent initial);