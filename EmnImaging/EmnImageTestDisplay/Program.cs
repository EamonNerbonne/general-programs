using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnImaging;
using System.Windows;

namespace EmnImageTestDisplay {
    class Program:Application {
        [STAThread]
        public static void Main(string[] args) {
            Application app = new Program();
            Window imgWin = new ImageWindow(ImageIO.Load(args[0]));
            
            
            app.Run(imgWin);
        }
    }
}
