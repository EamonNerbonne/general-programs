#ifndef ENVELOPES_H
#define ENVELOPES_H

#include "pamImage.h"

// ----------------------------------------------------------------------------- : Envelopes

/// The highest pixel in each column, or -1 for empty columns
std::vector<int> upper_envelope(PamImage<BWPixel> const& im);
/// The lowest pixel in each column, or -1 for empty columns
std::vector<int> lower_envelope(PamImage<BWPixel> const& im);

/// Draw an envelope image
PamImage<BWPixel> draw_envelope(std::vector<int>, int height);
/// 'blur' an envelope by taking the minimum/maximum over a certain width
std::vector<int> blur_envelope(std::vector<int> envelope, bool upper, int width);

/// The y positions of all up-facting pixels, sorted
std::vector<int> up_facing(PamImage<BWPixel> const& im);
/// The y positions of all down-facting pixels, sorted
std::vector<int> down_facing(PamImage<BWPixel> const& im);


/// An image coordinate
/** Coordinates are between pixels, so (0,0) is the top-left, and (width,height) is the bottom right */
struct Coord {
    int x,y;
    
    inline Coord() {}
    inline Coord(int x, int y) : x(x), y(y) {}
};

/// The boundary of a region of ink
class Boundary {
  public:
    inline int size() const { return (int)points.size(); }
    Coord operator [] (int i) const; // i can safely be outside [0..size)
    
    /// Angle along the boundary between point i and point j
    /** In the range (-pi,pi] */
    double angle(int i, int j) const;
    
  private:
    std::vector<Coord> points;
    void trace_boundary(int x, int y, int width, int height, PamImage<BWPixel> & im);
    friend void boundaries(PamImage<BWPixel> const & im, std::vector<Boundary>& out);
};

/// Find all boundaries in an image
void boundaries(PamImage<BWPixel> const & im, std::vector<Boundary>& out);

#endif