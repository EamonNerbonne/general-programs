using System.Windows;
using System.Windows.Input;

namespace HwrSplitter.Gui
{
    class ZoomRectManager
    {
    	readonly ZoomRect zoomRect;
    	readonly MainManager man;
        public ZoomRectManager(MainManager man, ZoomRect zoomRect) {
            this.zoomRect = zoomRect;
            this.man = man;
            man.Window.ImageAnnotViewbox.MouseLeave += imgView_MouseLeave;
            zoomRect.zoomRect.MouseLeftButtonDown += zoomRect_MouseLeftButtonDown;

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
