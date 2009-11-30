//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Windows;

//namespace EmnExtensions.Wpf.Plot.VizEngines
//{
//    class VizPixelScatterGeom : IPlotViz<Point[]>
//    {
//        VizGeometry impl;
//        public void DataChanged(Point[] newData)		{			impl.DataChanged(GraphUtils.PointCloud(newData));		}

//        public void SetOwner(IPlot<Point[]> owner)
//        {
//            impl.SetOwner(owner);
//        }

//        #endregion

//        #region IPlotViz Members

//        public Rect DataBounds
//        {
//            get { throw new NotImplementedException(); }
//        }

//        public Thickness Margin
//        {
//            get { throw new NotImplementedException(); }
//        }

//        public void DrawGraph(System.Windows.Media.DrawingContext context)
//        {
//            throw new NotImplementedException();
//        }

//        public void SetTransform(System.Windows.Media.Matrix boundsToDisplay, Rect displayClip)
//        {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }
//}
