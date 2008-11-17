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
using LastFmMdsDisplay;
using Microsoft.Win32;
using System.IO;

namespace SongPlotter
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        EmbeddingManager man;
        public Window1() {
            InitializeComponent();
            man = new EmbeddingManager();
        }

        private void loadbutton_Click(object sender, RoutedEventArgs e) {
            try {

                var openDialog = new OpenFileDialog() {
                    Title = "Load playlist ...",
                    Filter = "M3U file|*.m3u",
                };
                if (openDialog.ShowDialog() == true) {

                    using (var m3ustream = File.OpenRead(openDialog.FileName)) {
                        PositionedSong[] songs;
                        string[] unknown;
                        PositionedTracks.PositionSongs(m3ustream, man.Tools, man.Mds, out songs, out unknown);
                        foreach (string title in unknown)
                            Console.WriteLine("Unknown: title"+title);
                        songCanvas.SetSongs(songs);
                    }
                        
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
