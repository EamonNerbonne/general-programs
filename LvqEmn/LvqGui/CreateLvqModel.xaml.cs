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
using System.Globalization;
using System.IO;
using EmnExtensions.MathHelpers;
using System.ComponentModel;

namespace LvqGui {


	/// <summary>
	/// Interaction logic for CreateLvqModel.xaml
	/// </summary>

	public partial class CreateLvqModel : UserControl {
		public ModelType[] ModelTypes { get { return (ModelType[])Enum.GetValues(typeof(ModelType)); } }
		public CreateLvqModel() {
			this.DataContext = new LvqModelParams();
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			((IHasSeed)DataContext).Reseed();
		}
	}
	public enum ModelType {
		G2m, Gsm, Gm
	}
	public class LvqModelParams : INotifyPropertyChanged, IHasSeed {


		public int Dimensions {
			get { return _Dimensions; }
			set {
				if (value < Dimensionality) throw new ArgumentException("Data dimensions must be no fewer than internal dimensions");
				if (!_Dimensions.Equals(value)) { _Dimensions = value; _propertyChanged("Dimensions"); }
			}
		}
		private int _Dimensions;


		public ModelType ModelType {
			get { return _ModelType; }
			set { if (!_ModelType.Equals(value)) { if (value != LvqGui.ModelType.Gm) Dimensionality = 2; _ModelType = value; _propertyChanged("ModelType"); } }
		}
		private ModelType _ModelType;

		public int Dimensionality {
			get { return _Dimensionality; }
			set {
				if (value < 1 || value > Dimensions) throw new ArgumentException("Internal dimensionality must be between 1 and the dimensions of the data.");
				if (_ModelType != LvqGui.ModelType.Gm && value !=2) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
				if (!_Dimensionality.Equals(value)) { _Dimensionality = value; _propertyChanged("Dimensionality"); } }
		}
		private int _Dimensionality;

		public int PrototypesPerClass {
			get { return _PrototypesPerClass; }
			set { if (!_PrototypesPerClass.Equals(value)) { _PrototypesPerClass = value; _propertyChanged("PrototypesPerClass"); } }
		}
		private int _PrototypesPerClass;

		public uint Seed {
			get { return _Seed; }
			set { if (!_Seed.Equals(value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;


		public LvqModelParams() {
			_ModelType = ModelType.G2m;
			_Dimensionality = 2;
			_Dimensions = 50;
			_PrototypesPerClass = 3;
			this.Reseed();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

	}
}
