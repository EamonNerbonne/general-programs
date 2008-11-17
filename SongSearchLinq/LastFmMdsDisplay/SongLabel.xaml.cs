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
using LastFMspider;

namespace LastFmMdsDisplay
{
    public partial class SongLabel
    {
        public SongLabel() {
            InitializeComponent();
        }
        SongRef song;
        public SongRef Song {
            get { return song; }
            set {
                song = value;
                artistLabel.Content = song.Artist;
                titleLabel.Content = song.Title;
                InvalidateVisual();
            }
        }
    }
}
