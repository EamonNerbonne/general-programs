﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Windows;

namespace HwrDataModel
{
    public abstract class ShearedBox
    {
        public double top;
        public double bottom;
        public double left;
        public double right;
        public double shear;
        public double BottomXOffset { get { return (bottom - top) * Math.Tan(-2 * Math.PI * shear / 360.0); } }
		public double XOffsetForYOffset(double yOffset) { return yOffset * Math.Tan(-2 * Math.PI * shear / 360.0); }
        protected ShearedBox() { }
		protected ShearedBox(double top, double bottom, double left, double right, double shear) { this.top = top; this.bottom = bottom; this.left = left; this.right = right; this.shear = shear; }
		protected ShearedBox(XElement fromXml) {
            top = (double)fromXml.Attribute("top");
            bottom = (double)fromXml.Attribute("bottom");
            left = (double)fromXml.Attribute("left");
            right = (double)fromXml.Attribute("right");
            shear = (double)fromXml.Attribute("shear");
        }

		public bool ContainsPoint(Point p) {
			if (p.Y < bottom && p.Y >= top) {
				double x = p.X + XOffsetForYOffset(p.Y - top);
				return p.X < right && p.X >= left;
			}
			return false;
		}
		public Point CenterPoint { get { return new Point((left + right) / 2.0 + XOffsetForYOffset((bottom - top) / 2.0), (bottom + top) / 2.0); } }

        protected IEnumerable<XAttribute> MakeXAttrs() {
            yield return new XAttribute("top", top);
            yield return new XAttribute("bottom", bottom);
            yield return new XAttribute("left", left);
            yield return new XAttribute("right", right);
            yield return new XAttribute("shear", shear);
        }

    }
}
