using System;
using System.Collections.Generic;
using System.Xml.Linq;

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
        public ShearedBox() { }
        public ShearedBox(double top, double bottom, double left, double right, double shear) { this.top = top; this.bottom = bottom; this.left = left; this.right = right; this.shear = shear; }
        public ShearedBox(XElement fromXml) {
            top = (double)fromXml.Attribute("top");
            bottom = (double)fromXml.Attribute("bottom");
            left = (double)fromXml.Attribute("left");
            right = (double)fromXml.Attribute("right");
            shear = (double)fromXml.Attribute("shear");
        }


        protected IEnumerable<XAttribute> MakeXAttrs() {
            yield return new XAttribute("top", top);
            yield return new XAttribute("bottom", bottom);
            yield return new XAttribute("left", left);
            yield return new XAttribute("right", right);
            yield return new XAttribute("shear", shear);
        }

    }
}
