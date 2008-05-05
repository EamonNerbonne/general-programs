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
using HWRsplitter;

namespace EmnImageTestDisplay {
    /// <summary>
    /// Interaction logic for WordDetail.xaml
    /// </summary>
    public partial class WordDetail : UserControl {
        public WordDetail() {
            InitializeComponent();
            lineVisual = (VisualBrush)lineView.Fill;
            lineVisual.Transform = new MatrixTransform(1, 0, 1, 1, 0, 0);

            intensBrush = (ImageBrush)intensityGraph.Fill;
            //intensBrush.Transform = new MatrixTransform(1, 0, -1, 1, 0, 0);
            intensBodyBrush = (ImageBrush)intensityBodyGraph.Fill;
            //intensBodyBrush.Transform = new MatrixTransform(1, 0, -1, 1, 0, 0);
            intensRowBrush = (ImageBrush)intensityRowGraph.Fill;
        }
        VisualBrush lineVisual;
        ImageBrush intensBrush,intensBodyBrush,intensRowBrush;
        public Canvas ToZoom {
            get { return (Canvas)lineVisual.Visual; }
            set {            lineVisual.Visual = value;        }
        }
        Rect imgRect = new Rect(0,0,1,1);

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
        Line bodyBotLine, bodyTopLine;
        public void WordDisplay(ImageAnnotViewbox imageView, TextLine textline, Word word) {
            if (textline.shearedsum != null) {
                intensBrush.ImageSource =ImgdataFromShearedSum(textline.words, word, textline.shearedsum);
                intensBodyBrush.ImageSource = ImgdataFromShearedSum(textline.words, word, textline.shearedbodysum);
                byte[] rowSumImgData= ByteArrFromFloatArr(textline.rowsum);

                rowSumImgData[textline.bodyTop*4] = 0;
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

                ToZoom.Children.Add(bodyBotLine);
                ToZoom.Children.Add(bodyTopLine);
            }


            wordContent.Content = DescribeLine(textline,word);
            wordContent2.Content = DescribeLine2(textline, word);
            imgRect = new Rect(
                textline.left + Math.Min(0, textline.BottomXOffset), //x
                textline.top, //y
                textline.right-textline.left+Math.Abs(textline.BottomXOffset),
                textline.bottom-textline.top);
            redisplay();
        }

        private object DescribeLine2(TextLine textline, Word word) {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Line: [{0:f2},{1:f2}), length={2:f2}\n",textline.left,textline.right,textline.right-textline.left);
            sb.AppendFormat("Word: [{0:f2},{1:f2}), length={2:f2}, est={3:f2} ~ \n", word.left, word.right, word.right - word.left, word.symbolBasedLength.len, Math.Sqrt(word.symbolBasedLength.var) );
            return sb.ToString();
        }

        private string DescribeLine(TextLine textline, Word word) {
            StringBuilder sb = new StringBuilder();
            foreach(Word wordinline in textline.words) {
                if(word==wordinline) {
                    sb.Append("{");
                    sb.Append(wordinline.text);
                    sb.Append("} ");
                }else {
                    sb.Append(wordinline.text);
                    sb.Append(" ");
                };
            }
            sb.AppendLine();
            string imgCost = word.imageBasedCost.ToString("f3");
            
            sb.AppendFormat("Cost: {0}, [l={1},m={2},r={3}]\n", imgCost,word.startLightness,word.lookaheadSum,word.endLightness);
            var costs = textline.words.Select(w=>w.imageBasedCost).Where(c=>c!=double.NaN).ToArray();
            sb.AppendFormat("LineQ: Mean: {0}, Worst: {1}", costs.Average(), costs.Max() );
            return sb.ToString();
        }

        

        private void redisplay() {
            double fillHeight = lineView.ActualHeight;
            double fillWidth = lineView.ActualWidth;
            bool lineTooWide = imgRect.Width / imgRect.Height > fillWidth / fillHeight;
            double shearOffsetHelper = fillHeight ;//hardcoded shift to look a little further right
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
            intensRowBrush.Viewbox = new Rect(
                0,
                 - padHeight / 2,
                1,
                imgRect.Height + padHeight);


        }


        private void lineView_SizeChanged(object sender, SizeChangedEventArgs e) {
            redisplay();
        }
    }
}
