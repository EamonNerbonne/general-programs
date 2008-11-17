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

namespace LastFmMdsDisplay
{
    /// <summary>
    /// Interaction logic for SongCanvas.xaml
    /// </summary>
    public partial class SongCanvas : UserControl
    {
        public SongCanvas() {
            semi = new SolidColorBrush(new Color { A = 100, R = 200, G = 150, B = 100 });
            semi.Freeze();
            InitializeComponent();
        }

        PositionedSong[] songs;
        Label[] labels;
        SolidColorBrush semi;
        Rect bounds;
        public void SetSongs(IEnumerable<PositionedSong> songsEnum) {
            this.songs = songsEnum.ToArray();
            if (labels != null) foreach (var label in labels) songCanvas.Children.Remove(label);
            labels=new Label[songs.Length];
            bounds = Rect.Empty;
            for (int i = 0; i < songs.Length; i++) {
                labels[i] = new Label { Content = songs[i].Song.Artist + "\n" + songs[i].Song.Title, HorizontalContentAlignment=HorizontalAlignment.Center , Background=semi};
                songCanvas.Children.Add(labels[i]);
                bounds.Union(songs[i].Position);
            }
            InvalidateVisual();
        }
        protected override Size ArrangeOverride(Size arrangeBounds) {
            Matrix transMat = Matrix.Identity;
            transMat.Translate(-bounds.X, -bounds.Y);
            transMat.Scale(arrangeBounds.Width / bounds.Width, arrangeBounds.Height / bounds.Height);
            if(songs!=null)
            for (int i = 0; i < songs.Length; i++) {
                Point songPos = transMat.Transform(songs[i].Position);
                Size labSize = labels[i].DesiredSize;
                double w = labels[i].DesiredSize.Width;
                double h = labels[i].DesiredSize.Height;
                songPos.X -= w / 2;
                songPos.Y -= h / 2;
                Canvas.SetLeft(labels[i], songPos.X);
                Canvas.SetTop(labels[i], songPos.Y);
            }
            return base.ArrangeOverride(arrangeBounds);
        }
    }


}
