#include "StdAfx.h"
#include "ImageProcessor.h"

namespace HwrLibCliWrapper {
	ImageStruct<signed char> ImageProcessor::preprocess(ImageStruct<unsigned int> image) {
		printf("%x\n",image[1000,2000]);
		PamImage<RGBPixel> cImg(image.Width,image.Height);
		int i=0;
		for(int y=0;y<image.Height;y++){
			for(int x=0;x<image.Width;x++) {
				cImg.pix(x,y) =  RGBPixel(image[x,y]);
				i++;
			}
		}
		printf("%x\n",cImg.pix(1000,2000));
		PamImage<BWPixel> cProcImg;
		cProcImg = preprocessLimited(cImg);


		return PamImageToStruct(cProcImg);	
	}

	ImageStruct<float> ImageProcessor::ExtractFeatures(ImageStruct<signed char> block, [Out] int % topOffRef) {
		using std::min;
		using std::max;
		PamImage<BWPixel> pi = StructToPamImage(block);
		int topOff;
		PamImage<double> featImg( featuresImage(pi,45.0f,topOff));
		topOffRef = topOff;
		ImageStruct<float> featImgScaled(featImg.getWidth(),featImg.getHeight());
		for(int y=0;y<featImg.getHeight();y++) {
			double minV,maxV;
			minV=maxV = featImg.pix(0,y);
			for(int x=1;x<featImg.getWidth();x++) {
				minV = min(featImg.pix(x,y),minV);
				maxV = max(featImg.pix(x,y),maxV);
			}
			for(int x=0;x<featImg.getWidth();x++) {
				featImgScaled[x,y] = (float)((featImg.pix(x,y) - minV) / (maxV-minV));
			}
		}
		return featImgScaled;
	}
}