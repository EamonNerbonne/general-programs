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
using System.ComponentModel;
using EmnExtensions.MathHelpers;
using LvqLibCli;

namespace LvqGui {
	/// <summary>
	/// Interaction logic for CreateDataSetStar.xaml
	/// </summary>
	public partial class CreateDataSetStar : UserControl {
		public CreateDataSetStar() {
			InitializeComponent();
			DataContext = new DataSetStarParams();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			((IHasSeed)DataContext).Reseed();
		}
	}

	public class DataSetStarParams : INotifyPropertyChanged, IHasSeed {

		public event PropertyChangedEventHandler PropertyChanged;


		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }


		public int Dimensions {
			get { return _Dimensions; }
			set { if (value < _ClusterDimensionality) throw new ArgumentException("Data needs at least one dimension and no fewer than the clusters' dimensions"); if (!_Dimensions.Equals(value)) { _Dimensions = value; _propertyChanged("Dimensions"); } }
		}
		private int _Dimensions;

		public int NumberOfClasses {
			get { return _NumberOfClasses; }
			set { if (value < 2) throw new ArgumentException("Need at least 2 classes to meaningfully train"); if (!_NumberOfClasses.Equals(value)) { _NumberOfClasses = value; _propertyChanged("NumberOfClasses"); } }
		}
		private int _NumberOfClasses;

		public int PointsPerClass {
			get { return _PointsPerClass; }
			set { if (value < 1) throw new ArgumentException("Need a positive number of points"); if (!_PointsPerClass.Equals(value)) { _PointsPerClass = value; _propertyChanged("PointsPerClass"); } }
		}
		private int _PointsPerClass;

		public int NumberOfClusters {
			get { return _NumberOfClusters; }
			set { if (value < 1) throw new ArgumentException("Need a positive number of clusters"); if (!_NumberOfClusters.Equals(value)) { _NumberOfClusters = value; _propertyChanged("NumberOfClusters"); } }
		}
		private int _NumberOfClusters;

		public int ClusterDimensionality {
			get { return _ClusterDimensionality; }
			set { if (value < 1 || value > _Dimensions) throw new ArgumentException("Cluster dimensionality must be a positive number less than the absolute dimensionality"); if (!_ClusterDimensionality.Equals(value)) { _ClusterDimensionality = value; _propertyChanged("ClusterDimensionality"); } }
		}
		private int _ClusterDimensionality;

		public double ClusterCenterDeviation {
			get { return _ClusterCenterDeviation; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!_ClusterCenterDeviation.Equals(value)) { _ClusterCenterDeviation = value; _propertyChanged("ClusterCenterDeviation"); } }
		}
		private double _ClusterCenterDeviation;

		public double IntraClusterClassRelDev {
			get { return _IntraClusterClassRelDev; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!_IntraClusterClassRelDev.Equals(value)) { _IntraClusterClassRelDev = value; _propertyChanged("IntraClusterClassRelDev"); } }
		}
		private double _IntraClusterClassRelDev;

		public bool RandomlyTransformFirst {
			get { return _RandomlyTransformFirst; }
			set { if (!_RandomlyTransformFirst.Equals(value)) { _RandomlyTransformFirst = value; _propertyChanged("RandomlyTransformFirst"); } }
		}
		private bool _RandomlyTransformFirst;

		public uint Seed {
			get { return _Seed; }
			set { if (!_Seed.Equals(value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;

		public string CreateLabel() {
			return "star-" + Dimensions + "D-" + NumberOfClasses + "*" + PointsPerClass + ":" + NumberOfClusters + "(" + ClusterDimensionality + "D" + (RandomlyTransformFirst ? "?" : "") + ")*" + ClusterCenterDeviation.ToString("f1") + "~" + IntraClusterClassRelDev.ToString("f1");
		}

		public DataSetStarParams() {
			_ClusterCenterDeviation = 2.5;
			_ClusterDimensionality = 2;
			_Dimensions = 50;
			_IntraClusterClassRelDev = 0.33;
			_NumberOfClasses = 3;
			_NumberOfClusters = 3;
			_PointsPerClass = 3000;
			_RandomlyTransformFirst = true;
			this.Reseed();
		}

		public LvqDataSetCli CreateDataset(uint globalPS, uint globalIS) {
			return LvqDataSetCli.ConstructStarDataset(CreateLabel(),
				SeedUtils.MakeSeedFunc(new[] { Seed, globalPS }),
				dims:Dimensions,
				starDims:ClusterDimensionality,
				numStarTails: NumberOfClusters,
				classCount:NumberOfClasses,
				pointsPerClass: PointsPerClass,
				starMeanSep: ClusterCenterDeviation,
				starClassRelOffset: IntraClusterClassRelDev); 
			//TODO:randomTransform option.
			//TODO:use instance seed.
		}
	}
}
