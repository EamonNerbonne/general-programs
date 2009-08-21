using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataIO;

namespace HwrSplitter.Gui
{
	/// <summary>
	/// Interaction logic for WordDetail.xaml
	/// </summary>
	public partial class WordDetail : UserControl
	{
		public WordDetail() {
			InitializeComponent();
			lineVisual = (VisualBrush)lineView.Fill;
			lineVisual.Transform = new SkewTransform(45.0, 0); //TODO: make skew correction variable.

			intensBrush = (ImageBrush)intensityGraph.Fill;
			intensBodyBrush = (ImageBrush)intensityBodyGraph.Fill;
			intensRowBrush = (ImageBrush)intensityRowGraph.Fill;
			featuresGraphBrush = (ImageBrush)featuresGraph.Fill;
			RenderOptions.SetBitmapScalingMode(featuresGraph, BitmapScalingMode.Fant);
		}

		public TextBlock WordSelectorTextBlock { get { return wordSelectorTextBlock; } }
		VisualBrush lineVisual;
		ImageBrush intensBrush, intensBodyBrush, intensRowBrush, featuresGraphBrush;
		public Canvas ToZoom { get { return (Canvas)lineVisual.Visual; } set { lineVisual.Visual = value; } }
		internal Rect imgRect = new Rect(0, 0, 1, 1);

		byte[] ByteArrFromFloatArr(float[] arr) {
			byte[] imgData = new byte[arr.Length * 4];
			int i = 0;
			foreach (var f in arr) {
				imgData[i++] = (byte)(255 * f);
				imgData[i++] = (byte)(255 * f);
				imgData[i++] = (byte)(255 * f);
				imgData[i++] = (byte)(255);
			}
			return imgData;
		}

		BitmapSource ImgdataFromShearedSum(Word[] linewords, Word targetword, float[] shearedsum) {
			var imgData = ByteArrFromFloatArr(shearedsum);
			foreach (Word lineword in linewords) {
				var l = 4 * (int)lineword.left;
				var r = 4 * (int)lineword.right;
				imgData[l] = 0; imgData[l + 1] = 255; imgData[l + 2] = 0;
				imgData[r] = 255; imgData[r + 1] = 0; imgData[r + 2] = 255;
				if (targetword == lineword) {
					imgData[l + 4] = 0; imgData[l + 1 + 4] = 255; imgData[l + 2 + 4] = 0;
					imgData[r + 4] = 255; imgData[r + 1 + 4] = 0; imgData[r + 2 + 4] = 255;
				}
			}
			return BitmapSource.Create(shearedsum.Length, 1, 96.0, 96.0, PixelFormats.Bgra32, null, imgData, imgData.Length); ;
		}
		Line bodyBotLine, bodyTopLine, wordBotLine, wordTopLine;
		Point featureComputeOffset;

		public void DisplayLine(TextLine textline, Word word) {
			intensBrush.ImageSource = ImgdataFromShearedSum(textline.words, word, textline.shearedsum);
			intensBodyBrush.ImageSource = ImgdataFromShearedSum(textline.words, word, textline.shearedbodysum);
			
			BitmapSource bmp;
			textline.Retrieve(out bmp, out featureComputeOffset);
			featuresGraphBrush.ImageSource = bmp;
			


			byte[] rowSumImgData = ByteArrFromFloatArr(textline.rowsum);

			rowSumImgData[textline.bodyTop * 4] = 0;
			rowSumImgData[textline.bodyTop * 4 + 1] = 255;
			rowSumImgData[textline.bodyTop * 4 + 2] = 0;

			rowSumImgData[textline.bodyBot * 4] = 0;
			rowSumImgData[textline.bodyBot * 4 + 1] = 255;
			rowSumImgData[textline.bodyBot * 4 + 2] = 0;

			intensRowBrush.ImageSource =
				BitmapSource.Create(1, textline.rowsum.Length, 96.0, 96.0, PixelFormats.Bgra32, null,
				  rowSumImgData, 4);//this uses the fact that horizontal or vertical lines
			//are only distinguishable by their stride!

			if (bodyBotLine != null) {
				ToZoom.Children.Remove(bodyBotLine);
				ToZoom.Children.Remove(bodyTopLine);
				ToZoom.Children.Remove(wordBotLine);
				ToZoom.Children.Remove(wordTopLine);
			}

			bodyTopLine = new Line {
				X1 = 0,
				X2 = textline.shearedsum.Length - 1,
				Y1 = textline.top + textline.bodyTop,
				Y2 = textline.top + textline.bodyTop,
				StrokeThickness = 2,
				Stroke = Brushes.Fuchsia
			};

			bodyBotLine = new Line {
				X1 = 0,
				X2 = textline.shearedsum.Length - 1,
				Y1 = textline.top + textline.bodyBot,
				Y2 = textline.top + textline.bodyBot,
				StrokeThickness = 2,
				Stroke = Brushes.HotPink
			};

			wordTopLine = new Line {
				X1 = word.left,
				X2 = word.right,
				Y1 = word.top,
				Y2 = word.top,
				StrokeThickness = 2,
				Stroke = Brushes.Blue
			};

			wordBotLine = new Line {
				X1 = word.left + word.BottomXOffset,
				X2 = word.right + word.BottomXOffset,
				Y1 = word.bottom,
				Y2 = word.bottom,
				StrokeThickness = 2,
				Stroke = Brushes.Blue
			};

			ToZoom.Children.Add(bodyBotLine);
			ToZoom.Children.Add(bodyTopLine);
			ToZoom.Children.Add(wordBotLine);
			ToZoom.Children.Add(wordTopLine);
		}

		internal void redisplay() {
			double fillHeight = lineView.ActualHeight;
			double fillWidth = lineView.ActualWidth;
			bool lineTooWide = imgRect.Width / imgRect.Height > fillWidth / fillHeight;
			double shearOffsetHelper = imgRect.Height;//hardcoded shift to look a little further right
			double padHeight = 0, padWidth = 0;
			if (lineTooWide)
				padHeight = fillHeight / fillWidth * imgRect.Width - imgRect.Height;
			else//too high...
				padWidth = fillWidth / fillHeight * imgRect.Height - imgRect.Width;

			lineVisual.Viewbox = new Rect(
				imgRect.X - padWidth / 2 + shearOffsetHelper
				+ padHeight / 2,//Special!  the padheight shift the image downward, which, due to the shear, also shifts
				//the image to the right - so - we need to look further to the right.
				imgRect.Y - padHeight / 2,
				imgRect.Width + padWidth,
				imgRect.Height + padHeight);
			intensBrush.Viewbox = intensBodyBrush.Viewbox =
				new Rect(
				imgRect.X - padWidth / 2 + shearOffsetHelper,
				0,
				imgRect.Width + padWidth,
				1);
			double featImgHeight = featuresGraphBrush.ImageSource == null ? 1 : featuresGraphBrush.ImageSource.Height;
			featuresGraphBrush.Viewbox = new Rect(
				imgRect.X - padWidth / 2 + shearOffsetHelper - featureComputeOffset.X,
				0,
				imgRect.Width + padWidth,
				featImgHeight);
			featuresGraph.Height = featImgHeight*2;
			intensRowBrush.Viewbox = new Rect(
				0,
				 -padHeight / 2,
				1,
				imgRect.Height + padHeight);
		}

		private void lineView_SizeChanged(object sender, SizeChangedEventArgs e) {
			redisplay();
		}

		internal void displayFeatures(TextLine textline) {
			//featuresGraphBrush.Viewbox(
			
		}
	}
}
