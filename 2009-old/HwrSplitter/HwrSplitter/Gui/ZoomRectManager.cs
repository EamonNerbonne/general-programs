using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;

namespace HwrSplitter.Gui
{
    class ZoomRectManager
    {
        ZoomRect zoomRect;
        MainManager man;
        public ZoomRectManager(MainManager man, ZoomRect zoomRect) {
            this.zoomRect = zoomRect;
            this.man = man;
            man.Window.ImageAnnotViewbox.MouseLeave += new MouseEventHandler(imgView_MouseLeave);
            zoomRect.zoomRect.MouseLeftButtonDown += new MouseButtonEventHandler(zoomRect_MouseLeftButtonDown);

        }
        void zoomRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Point clickTarget = e.MouseDevice.GetPosition(zoomRect);
            double xRel = clickTarget.X / zoomRect.ActualWidth;
            double yRel = clickTarget.Y / zoomRect.ActualHeight;
            double xAbs = zoomRect.zoomViewBrush.Viewbox.X + xRel * zoomRect.zoomViewBrush.Viewbox.Width;
            double yAbs = zoomRect.zoomViewBrush.Viewbox.Y + yRel * zoomRect.zoomViewBrush.Viewbox.Height;

            man.SelectPoint(new Point(xAbs, yAbs));
            zoomRect.ShowNewPoint(man.LastClickPoint);
            //position is relative?
        }

        void imgView_MouseLeave(object sender, MouseEventArgs e) {
            zoomRect.ShowNewPoint(man.LastClickPoint);
            //TODO: this should also happen when WordDetail is updated.
        }

    }
}
