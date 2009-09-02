﻿using System;

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
using HwrDataModel;
using HwrSplitter.Engine;

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

			featuresGraphBrush = (ImageBrush)featuresGraph.Fill;
			RenderOptions.SetBitmapScalingMode(featuresGraph, BitmapScalingMode.Fant);
		}

		public TextBlock WordSelectorTextBlock { get { return wordSelectorTextBlock; } }
		VisualBrush lineVisual;
		ImageBrush featuresGraphBrush;
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
		Point featureComputeOffset;

		public void DisplayLine(TextLineCostOptimizer tlco, HwrPageImage pageImage, TextLine textline, Word word) {
			//intensBrush.ImageSource = ImgdataFromShearedSum(textline.words, word, textline.shearedsum);
			//intensBodyBrush.ImageSource = ImgdataFromShearedSum(textline.words, word, textline.shearedbodysum);

			BitmapSource bmp;
			tlco.ComputeFeatures(pageImage, textline, out bmp, out featureComputeOffset);
			featuresGraphBrush.ImageSource = bmp;
			foreach (var line in ToZoom.Children.OfType<Line>().Where(line => line.Tag == this).ToArray())
				ToZoom.Children.Remove(line);


			Line[] lines = new[] {
				new Line {//text-line top edge
				X1 = textline.left,
				X2 = textline.right,
				Y1 = textline.top,
				Y2 = textline.top,
				StrokeThickness = 2,
				Stroke = Brushes.Red
			}, new Line {//text-line bottom edge
				X1 = textline.left + textline.BottomXOffset,
				X2 = textline.right + textline.BottomXOffset,
				Y1 = textline.bottom,
				Y2 = textline.bottom,
				StrokeThickness = 2,
				Stroke = Brushes.Red
			}, new Line {//text-body top line
				X1 = textline.left + textline.XOffsetForYOffset(textline.bodyTop),
				X2 = textline.right + textline.XOffsetForYOffset(textline.bodyTop),
				Y1 = textline.top + textline.bodyTop,
				Y2 = textline.top + textline.bodyTop,
				StrokeThickness = 2,
				Stroke = Brushes.Fuchsia
			}, new Line {//text-body bottom line
				X1 = textline.left + textline.XOffsetForYOffset(textline.bodyBot),
				X2 = textline.right + textline.XOffsetForYOffset(textline.bodyBot), 
				Y1 = textline.top + textline.bodyBot,
				Y2 = textline.top + textline.bodyBot,
				StrokeThickness = 2,
				Stroke = Brushes.HotPink
			}, new Line { //text-line(s) left edge
				X1 = textline.left,
				X2 = textline.left,
				Y1 = 0,
				Y2 = pageImage.Height,
				StrokeThickness = 2,
				Stroke = Brushes.LightSalmon
			}, new Line { //text-line(s) right edge
				X1 = textline.right,
				X2 = textline.right,
				Y1 = 0,
				Y2 = pageImage.Height,
				StrokeThickness = 2,
				Stroke = Brushes.LightSalmon
			}, 
			new Line { //text-line(s) left edge
				X1 = textline.OuterExtremeLeft,
				X2 = textline.OuterExtremeLeft + textline.BottomXOffset,
				Y1 = textline.top,
				Y2 = textline.bottom,
				StrokeThickness = 2,
				Stroke = Brushes.Red
			}, new Line { //text-line(s) right edge
				X1 = textline.OuterExtremeRight,
				X2 = textline.OuterExtremeRight + textline.BottomXOffset,
				Y1 = textline.top,
				Y2 = textline.bottom,
				StrokeThickness = 2,
				Stroke = Brushes.Red
			}, 
			};
			foreach (var line in lines) {
				line.Tag = this;
				ToZoom.Children.Add(line);
			}
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
			double featImgHeight = featuresGraphBrush.ImageSource == null ? 1 : featuresGraphBrush.ImageSource.Height;
			featuresGraphBrush.Viewbox = new Rect(
				imgRect.X - padWidth / 2 + shearOffsetHelper - featureComputeOffset.X,
				0,
				imgRect.Width + padWidth,
				featImgHeight);
			featuresGraph.Height = featImgHeight * 2;
		}

		private void lineView_SizeChanged(object sender, SizeChangedEventArgs e) {
			redisplay();
		}

		internal void displayFeatures(TextLine textline) {
			//featuresGraphBrush.Viewbox(

		}
	}
}
