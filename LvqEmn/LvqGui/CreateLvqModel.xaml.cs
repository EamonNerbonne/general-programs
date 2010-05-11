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

	}
	public enum ModelType {
		G2m, Gsm, Gm
	}
	public class LvqModelParams {
		public ModelType ModelType { get; set; }
		public int Dimensionality { get; set; }
		public int PrototypesPerClass { get; set; }
	}

}
