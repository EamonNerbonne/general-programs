using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LvqDataModel
{
	public class PointCloudSettings
	{
		public int Dimensions { get; set; }
		public int NumberOfSets { get; set; }
		public double StddevOfCloudCenters { get; set; }
		public int PointsPerCloud { get; set; }
	}
}
