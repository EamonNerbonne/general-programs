#pragma once

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
}