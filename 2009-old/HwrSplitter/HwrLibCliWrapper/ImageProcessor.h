#pragma once

#include "ImageStruct.h"
#include "image/preprocess.h"

namespace HwrLibCliWrapper {

	public ref class ImageProcessor
	{
	public:
		static ImageStruct<signed char> preprocess(ImageStruct<unsigned int> image);

		static ImageStruct<float> ExtractFeatures(ImageStruct<signed char> block, [Out] int % topOffRef);

		template<typename T> static ImageStruct<T> PamImageToStruct(PamImage<T> pi) {
			ImageStruct<T> retval =  ImageStruct<T>(pi.getWidth(),pi.getHeight());

			for(int y=0;y<retval.Height;y++){
				for(int x=0;x<retval.Width;x++) {
					retval[x,y] = pi.pix(x,y);
				}
			}
			return retval;
		}

		template<typename T> static PamImage<T> StructToPamImage(ImageStruct<T> is) {
			PamImage<T> retval(is.Width,is.Height);

			for(int y=0;y<retval.getHeight();y++){
				for(int x=0;x<retval.getWidth();x++) {
					retval.pix(x,y) = is[x,y];
				}
			}
			return retval;
		}
	};
}