// HwrLibCliWrapper.h

#pragma once
#include "Stdafx.h"
#pragma warning (disable:4482)
#include "image/preprocess.h"
#include "feature/featurevector.h"
#include "WordSplitSolver.h"
#include "FeatureDistribution.h"
#include "image/transformations.h"



using namespace System::Runtime::InteropServices;



namespace HwrLibCliWrapper {
	generic <typename T> public value class ImageStruct {
		array<T>^ data;
		int width;
		int height;
	public:
		property int Width {int get(){return width;}}
		property int Height {int get(){return height;}}
		property array<T>^ RawData {array<T>^ get() {return data;}}
		property int Stride {int get(){return width*sizeof(T);}}
		property T default[int, int] {
			T get(int x, int y){return data[y*width+x];} 
			void set(int x, int y, T val) {data[y*width+x]=val;}
		}

		property bool IsInitialized {bool get(){return data==nullptr;}}
		ImageStruct(int width, int height):width(width),height(height),data(gcnew array<T>(height*width)) {}

		ImageStruct<T> CropTo(int x0, int y0, int x1,int y1) {
			ImageStruct<T> ret(x1-x0,y1-y0);
			for(int y=y0;y<y1;y++)
				for(int x=x0;x<x1;x++)
					ret[x-x0,y-y0] = default[x,y];
			return ret;
		}

		generic <typename U>
		ImageStruct<U> MapTo(Func<T,U>^ mapF) {
			ImageStruct<U> ret(Width,Height);
			for(int y=0;y<Height;y++)
				for(int x=0;x<Width;x++)
					ret[x,y] = mapF->Invoke(default[x,y]);
			return ret;
		}
	};


	public ref class ImageProcessor
	{
	public:
		static ImageStruct<signed char> preprocess(ImageStruct<unsigned int> image) {
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

		static ImageStruct<float> ExtractFeatures(ImageStruct<signed char> block, [Out] int % topOffRef) {
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



		template<typename T>
		static ImageStruct<T> PamImageToStruct(PamImage<T> pi) {
			ImageStruct<T> retval =  ImageStruct<T>(pi.getWidth(),pi.getHeight());

			for(int y=0;y<retval.Height;y++){
				for(int x=0;x<retval.Width;x++) {
					retval[x,y] = pi.pix(x,y);
				}
			}
			return retval;
		}

		template<typename T>
		static PamImage<T> StructToPamImage(ImageStruct<T> is) {
			PamImage<T> retval(is.Width,is.Height);

			for(int y=0;y<retval.getHeight();y++){
				for(int x=0;x<retval.getWidth();x++) {
					retval.pix(x,y) = is[x,y];
				}
			}
			return retval;
		}
	};
	public ref class HwrOptimizer {
		AllSymbolClasses* symbols;
		!HwrOptimizer() {
			delete symbols;
			symbols = NULL;
		}
		~HwrOptimizer() { this->!HwrOptimizer(); }
	public:
		HwrOptimizer(array<HwrDataModel::SymbolClass^>^ symbolClasses) : symbols(new AllSymbolClasses(symbolClasses->Length )) {
			symbols->initRandom();
		}

		array<int>^ SplitWords(ImageStruct<signed char> block, array<unsigned> ^ sequenceToMatch, [Out] int % topOffRef,float shear) {
			using std::min;
			using std::max;
			using std::cout;
			using std::wcout;
			boost::timer t;

			PamImage<BWPixel> shearedImg = ImageProcessor::StructToPamImage(block);
			ImageBW unsheared = unshear(shearedImg,shear);
			topOffRef = shearedImg.getWidth() - unsheared.getWidth();
			ImageFeatures feats(unsheared);
			vector<short> sequenceVector;
			for(int i=0;i<sequenceToMatch->Length;i++) {
				unsigned tmp = sequenceToMatch[i];
				sequenceVector.push_back(tmp);
			}

			cout << "C++ textline prepare took " << t.elapsed() <<"\n";
			WordSplitSolver splitSolve( *symbols, feats, sequenceVector);

			vector<int> splits = splitSolve.MostLikelySplit();
			array<int>^ retval = gcnew array<int>((int)splits.size());
			for(int i=0;i<(int)splits.size();i++) {
				retval[i] = splits[i];
			}

			splitSolve.Learn();

			return retval;
		}
	};
}

