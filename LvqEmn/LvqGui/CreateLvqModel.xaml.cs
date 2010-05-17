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

namespace LvqGui
{


	/// <summary>
	/// Interaction logic for CreateLvqModel.xaml
	/// </summary>

	public partial class CreateLvqModel : UserControl
	{
		public ModelType[] ModelTypes { get { return (ModelType[])Enum.GetValues(typeof(ModelType)); } }
		public CreateLvqModel()
		{
			this.DataContext = new LvqModelParams();
			InitializeComponent();
		}
	}
	public enum ModelType
	{
		G2m, Gsm, Gm
	}
	public class LvqModelParams : INotifyPropertyChanged
	{
		public ModelType ModelType
		{
			get { return _ModelType; }
			set { if (!_ModelType.Equals(value)) { if (value != LvqGui.ModelType.Gm) Dimensionality = 2; _ModelType = value; PropertyChanged(this, new PropertyChangedEventArgs("ModelType")); } }
		}
		private ModelType _ModelType;
		

		public int Dimensionality
		{
			get { return _Dimensionality; }
			set { if (!_Dimensionality.Equals(value)) { _Dimensionality = value; PropertyChanged(this, new PropertyChangedEventArgs("Dimensionality")); } }
		}
		private int _Dimensionality;

		public int PrototypesPerClass
		{
			get { return _PrototypesPerClass; }
			set { if (!_PrototypesPerClass.Equals(value)) { _PrototypesPerClass = value; PropertyChanged(this, new PropertyChangedEventArgs("PrototypesPerClass")); } }
		}
		private int _PrototypesPerClass;

		public uint Seed
		{
			get { return _Seed; }
			set { if (!_Seed.Equals(value)) { _Seed = value; PropertyChanged(this, new PropertyChangedEventArgs("Seed")); } }
		}
		private uint _Seed;

		public LvqModelParams() { Seed = RndHelper.MakeSecureUInt(); ModelType = ModelType.G2m; Dimensionality = 2; PrototypesPerClass = 3; }

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
