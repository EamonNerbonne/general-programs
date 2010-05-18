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

namespace LvqGui {
	/// <summary>
	/// Interaction logic for CreateDataSet.xaml
	/// </summary>
	public partial class CreateDataSet : UserControl {
		public CreateDataSet() {
			InitializeComponent();
		}
	}

	class DataSetParams : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;


		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }


		public int Dimensions {
			get { return _Dimensions; }
			set { if (value < 1) throw new ArgumentException("Need at least one dimension"); if (!_Dimensions.Equals(value)) { _Dimensions = value; _propertyChanged("Dimensions"); } }
		}
		private int _Dimensions;



		public int NumberOfClasses {
			get { return _NumberOfClasses; }
			set { if (value < 2) throw new ArgumentException("Cannot meaningfully train classifier on fewer than 2 classes"); if (!_NumberOfClasses.Equals(value)) { _NumberOfClasses = value; _propertyChanged("NumberOfClasses"); } }
		}
		private int _NumberOfClasses;



		public int PointsPerClass {
			get { return _PointsPerClass; }
			set { if (value < 1) throw new ArgumentException("Each class needs at least 1 training sample"); if (!_PointsPerClass.Equals(value)) { _PointsPerClass = value; _propertyChanged("PointsPerClass"); } }
		}
		private int _PointsPerClass;


		public double ClassCenterDeviation {
			get { return _ClassCenterDeviation; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!_ClassCenterDeviation.Equals(value)) { _ClassCenterDeviation = value; _propertyChanged("ClassCenterDeviation"); } }
		}
		private double _ClassCenterDeviation;

		public uint Seed {
			get { return _Seed; }
			set { if (!_Seed.Equals(value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;

		public DataSetParams() {
			_Seed = RndHelper.MakeSecureUInt();
			_PointsPerClass = 2000;
			_NumberOfClasses = 3;
			_Dimensions = 16;
			_ClassCenterDeviation = 2.5;
		}
	}
}
