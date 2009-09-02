//+----------------------------------------------------------------------------+
//| Handwriting recognition - Image tools                                      |
//| Morphological skeleton                                                     |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes

#include "stdafx.h"
#include "boost/pool/pool_alloc.hpp"
#include "image/pamImage.h"
//#define SKEL_DBG_PRINT_STACK_SIZE

using namespace std;

// ----------------------------------------------------------------------------- : Types etc.

// Based on:
//   An augmented Fast Marching Method for computing skeletons and centerlines
//   Alexandru Telea and Jarke J. van Wijk
//   ACM International Conference Proceeding Series; Vol. 22
//   http://portal.acm.org/citation.cfm?id=509782

/// Points, with associated t value
struct Point {
	int    x,y;
	double t;
	Point() {}
	Point(int x, int y, double t) : x(x), y(y), t(t) {}
	inline bool operator < (const Point& p) const {
		return t > p.t;
	}
};

struct Skeletonizer {
	PamImage<BWPixel> const original;
	int width, height;
	
	double  last_known; // t values <= this are known
	double* t;    // t = distance transform result
	double* u[2]; // u = label of closest boundary. Two u's, first is from top, second from bottom
	
	inline size_t at(int y, int x) {
		return (y+1) * (width+2) + (x+1);
	}
	
	priority_queue<Point> queue;
	
	// memory
	Skeletonizer(PamImage<BWPixel> const& im);
	~Skeletonizer();
	// initialization
	void init();
	void try_init(int dir, int x, int y, int& unique1, int uu,int& maxSize);
	// propagation
	void propagate();
	void propagate(size_t from, int x, int y);
	void collect_u(int x, int y, double* sums, double* mins, double* maxs, int& count);
	void collect_t(int x1, int y1, int x2, int y2, double& sol);
	// skeleton & other results
	bool on_skeleton(size_t p1, size_t p2, double treshold, bool fat);
	void skeleton(PamImage<BWPixel> out, double treshold, bool fat);
	void distance_transform(PamImage<GrayPixel> out);
};

static const double UNKNOWN_U = 1e128;
static const double LARGE = 1e128;

// ----------------------------------------------------------------------------- : Memory management

Skeletonizer::Skeletonizer(PamImage<BWPixel> const& im) :original(im) {
	width  = im.getWidth();
	height = im.getHeight();
	last_known = LARGE;
	// alloc arrays, init as unknown
	size_t size = (width+2)*(height+2);
	t    = new double[size];
	u[0] = new double[size];
	u[1] = new double[size];
	for (size_t i = 0 ; i < size ; ++i) {
		u[0][i] = u[1][i] = UNKNOWN_U;
		t[i] = 0.0;
	}
}
Skeletonizer::~Skeletonizer() {
	delete [] t;
	delete [] u[0];
	delete [] u[1];
}

// ----------------------------------------------------------------------------- : Initialization

// Initialization: march around all boundaries, assign unique u values
void Skeletonizer::init() {
	int unique1 = 0;
	int unique2 = 0;
	int maxSize=0;
	for (int y = 0 ; y < height ; ++y) {
		for (int x = 0 ; x < width ; ++x) {
			if (original.pix(x,y)) {
				t[at(y,x)] = LARGE; // foreground pixels are unknown, background is 0.0
			} else {
				if (y + 1 < height && original.pix(x,y+1)) {
					try_init(6,x,y,unique1,0,maxSize);
				}
				if (y > 0 && original.pix(x,y-1)) {
					try_init(2,x,y,unique2,1,maxSize);
				}
			}
		}
	}
#ifdef SKEL_DBG_PRINT_STACK_SIZE
	printf("max stack size:%d\n", maxSize);
#endif
}

//TODO: custom stack impl.

void Skeletonizer::try_init(int dir_p, int x_p, int y_p, int& unique, int uu, int&maxSize) {
	using namespace boost;
	struct SkelPoint {
		int dir, x,y;
		SkelPoint(int dir,int x,int y):dir(dir),x(x),y(y){}
	};
	//vector<SkelPoint, pool_allocator<SkelPoint> > backStore(50000);
	stack<SkelPoint
		, vector<SkelPoint>
	> todo;
	//const_cast< vector<SkelPoint, pool_allocator<SkelPoint> > & >( todo._Get_container()).reserve(10000);
	todo.push(SkelPoint(dir_p,x_p,y_p));

	while(!todo.empty()) {
#ifdef SKEL_DBG_PRINT_STACK_SIZE
		if(todo.size()>maxSize) maxSize = (int)todo.size();
#endif
		SkelPoint& nextP = todo.top();
		int dir = nextP.dir;
		int x = nextP.x;
		int y = nextP.y;
		todo.pop();

		if (x < -1 || x > width || y < -1 || y > height) continue; // outside 1px extra border
		//if (x < 0 || x >= width || y < 0 || y >= height) return; // outside image
		if (u[uu][at(y,x)] != UNKNOWN_U) continue;
		if (x >= 0 && x < width && y >= 0 && y < height && original.pix(x,y)) continue; // foreground
		if (!( (y > 0          && x >= 0 && x < width  && original.pix(x,y-1)) ||
			   (x > 0          && y >= 0 && y < height && original.pix(x-1,y)) ||
			   (y + 1 < height && x >= 0 && x < width  && original.pix(x,y+1)) ||
			   (x + 1 < width  && y >= 0 && y < height && original.pix(x+1,y)))) continue; // no foreground 4neighbour

		u[uu][at(y,x)] = unique++;
		if (uu == 0) {
			// enqueue boundary point
			queue.push(Point(x,y,0));
		}
		// try all 8neighbours, in counter-clockwise order, starting with the direction we came from
		for (int dd = 0 ; dd < 8 ; ++dd) {
			int d = (dd + dir) % 8;
			if (d == 7) todo.push(SkelPoint(3, x+1, y+1));
			if (d == 6) todo.push(SkelPoint(2, x  , y+1));
			if (d == 5) todo.push(SkelPoint(1, x-1, y+1));
			if (d == 4) todo.push(SkelPoint(0, x-1, y  ));
			if (d == 3) todo.push(SkelPoint(7, x-1, y-1));
			if (d == 2) todo.push(SkelPoint(6, x  , y-1));
			if (d == 1) todo.push(SkelPoint(5, x+1, y-1));
			if (d == 0) todo.push(SkelPoint(4, x+1, y  ));
		}
	}
}

// ----------------------------------------------------------------------------- : Propagation

void Skeletonizer::propagate() {
	while (!queue.empty()) {
		Point p = queue.top(); queue.pop();
		last_known = p.t;
		// for each neighbour
		size_t from = at(p.y, p.x);
		propagate(from, p.x+1, p.y);
		propagate(from, p.x-1, p.y);
		propagate(from, p.x, p.y+1);
		propagate(from, p.x, p.y-1);
	}
}

void Skeletonizer::propagate(size_t from, int x, int y) {
	if (x < -1 || x > width || y < -1 || y > height) return;
	size_t yx = at(y,x);
	if (t[yx] <= last_known) return; // already known
	// the first time we visit this x,y? update u
	if (u[0][yx] == UNKNOWN_U) {
		double sums[2]={0,0}, mins[2]={1e100,1e100}, maxs[2]={-1e100,-1e100};
		int count = 0;
		// known neighbours of (x,y)
		collect_u(x-1, y, sums,mins,maxs, count);
		collect_u(x+1, y, sums,mins,maxs, count);
		collect_u(x, y-1, sums,mins,maxs, count);
		collect_u(x, y+1, sums,mins,maxs, count);
		// take avarage, except across creases (distance > sqrt(2))
		u[0][yx] = (maxs[0] - mins[0] < 1.5) ? sums[0]/count : u[0][from];
		u[1][yx] = (maxs[1] - mins[1] < 1.5) ? sums[1]/count : u[1][from];
	}
	// always: update t
	double old_t = t[yx];
	collect_t(x-1, y,  x, y-1,  t[yx]);
	collect_t(x+1, y,  x, y-1,  t[yx]);
	collect_t(x-1, y,  x, y+1,  t[yx]);
	collect_t(x+1, y,  x, y+1,  t[yx]);
	if (t[yx] < old_t) {
		queue.push(Point(x,y,t[yx]));
	}
	
}
void Skeletonizer::collect_u(int x, int y, double* sums, double* mins, double* maxs, int& count) {
	if (x < -1 || x > width || y < -1 || y > height) return;
	//if (x < 0 || x >= width || y < 0 || y >= height) return;
	if (t[at(y,x)] > last_known) return; // not known
	count++;
	for (int uu = 0 ; uu <= 1 ; ++uu) {
		double v = u[uu][at(y,x)];
		sums[uu] += v;
		mins[uu] = min(mins[uu],v);
		maxs[uu] = max(maxs[uu],v);
	}
}

void Skeletonizer::collect_t(int x1, int y1, int x2, int y2, double& sol) {
	size_t yx1 = at(y1,x1), yx2 = at(y2,x2);
	bool know1 = x1 >= -1 && x1 <= width && y1 >= -1 && y1 <= height  &&  t[yx1] <= last_known;
	bool know2 = x2 >= -1 && x2 <= width && y2 >= -1 && y2 <= height  &&  t[yx2] <= last_known;
	//bool know1 = x1 >= 0 && x1 < width && y1 >= 0 && y1 < height  &&  t[yx1] <= last_known;
	//bool know2 = x2 >= 0 && x2 < width && y2 >= 0 && y2 < height  &&  t[yx2] <= last_known;
	// use as much known information as possible
	if (know1 && know2) {
		double r = sqrt(max(0., 2 - sqr(t[yx1] - t[yx2])));
		double s = (t[yx1] + t[yx2] - r) / 2;
		if (s >= t[yx1] && s >= t[yx2]) {
			sol = min(sol, s);
		} else {
			s += r;
			if (s >= t[yx1] && s >= t[yx2]) {
				sol = min(sol, s);
			}
		}
	} else if (know1) {
		sol = min(sol, 1 + t[yx1]);
	} else if (know2) {
		sol = min(sol, 1 + t[yx2]);
	}
}

// ----------------------------------------------------------------------------- : Skeleton

bool Skeletonizer::on_skeleton(size_t p1, size_t p2, double treshold, bool fat) {
	if (abs(u[0][p1] - u[0][p2]) < treshold) return false; // below threshold
	if (abs(u[1][p1] - u[1][p2]) < treshold) return false;
	if (!fat && t[p1] < t[p2]) return false; // single pixel thick
	if (!fat && t[p1] == t[p2] && p1<p2) return false;
	return true;
}

void Skeletonizer::skeleton(PamImage<BWPixel> out, double treshold, bool fat) {
	for (int y = 0 ; y < height ; ++y) {
		for (int x = 0 ; x < width ; ++x) {
			bool on = original.pix(x,y)
			       && (on_skeleton(at(y,x), at(y-1,x), treshold, fat)
			        || on_skeleton(at(y,x), at(y+1,x), treshold, fat)
			        || on_skeleton(at(y,x), at(y,x-1), treshold, fat)
			        || on_skeleton(at(y,x), at(y,x+1), treshold, fat));
			out.pix(x,y) = on ? 1 : 0;
		}
	}
}

void Skeletonizer::distance_transform(PamImage<GrayPixel>  out) {
	// find max
	double tmax = 0;
	for (int y = 0 ; y < height ; ++y) {
		for (int x = 0 ; x < width ; ++x) {
			tmax = max(tmax, t[at(y,x)]);
			//if (u[0][at(y,x)] != UNKNOWN_U) tmax = max(tmax, u[0][at(y,x)]);
			//if (u[1][at(y,x)] != UNKNOWN_U) tmax = max(tmax, u[1][at(y,x)]);
		}
	}
	// copy
	for (int y = 0 ; y < height ; ++y) {
		for (int x = 0 ; x < width ; ++x) {
			out.pix(x,y) = (GrayPixel)floor(t[at(y,x)] * 255.9999 / tmax);
			//out.pix(x,y) = (GrayPixel)floor(u[0][at(y,x)] * 255.9999 / tmax);
			//out.pix(x,y) = (GrayPixel)floor(u[1][at(y,x)] * 255.9999 / tmax);
		}
	}
}

// ----------------------------------------------------------------------------- : Driver

PamImage<BWPixel> skeleton(PamImage<BWPixel> const& im, double threshold, bool fat) {
	Skeletonizer s(im);
	s.init();
	s.propagate();
	PamImage<BWPixel> im2(s.width, s.height);
	s.skeleton(im2, threshold, fat);
	return im2;
}

PamImage<GrayPixel> distance_transform(PamImage<BWPixel> const& im) {
	Skeletonizer s(im);
	s.init();
	s.propagate();
	PamImage<GrayPixel> im2(s.width, s.height);
	s.distance_transform(im2);
	return im2;
}
