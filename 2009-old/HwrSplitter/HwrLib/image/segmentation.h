#ifndef SEGMENTATION_H
#define SEGMENTATION_H
#include "../stdafx.h"

#include "pamImage.h"
// ----------------------------------------------------------------------------- : Segmentation

class ImageComponent {
  public:
	ImageComponent() {}
	ImageComponent(RGBPixel label);
	RGBPixel label; ///< Color this component is labeled with
	long min_x, max_x;
	long min_y, max_y;
	long min_xy, max_xy; ///< x+y
	double mean_xy;
	long size; ///< Number of pixels in the component
};

/// An image segmented into connected coponents.
/// Each component has a different color
class SegmentedImage : public PamImage<RGBPixel> {
  public:
	SegmentedImage(PamImage<BWPixel> const& im);
	
	/// The connected components
	std::vector<ImageComponent> components;
	
	/// Change the color of a component a to color b
	void recolor(const ImageComponent& a, RGBPixel b);
	
  private:
	// Pick a new color not already used
	RGBPixel newColor();
	void floodfill(long x, long y, PamImage<BWPixel> const& im, ImageComponent& component);
};

#endif