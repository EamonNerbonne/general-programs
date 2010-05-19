﻿using System;
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
using LvqLibCli;
using System.Threading;

namespace LvqGui {


	/// <summary>
	/// Interaction logic for CreateLvqModel.xaml
	/// </summary>

	public partial class CreateLvqModel : UserControl {
		public ModelType[] ModelTypes { get { return (ModelType[])Enum.GetValues(typeof(ModelType)); } }
		public CreateLvqModel() { InitializeComponent(); }

		private void Button_Click(object sender, RoutedEventArgs e) {
			((IHasSeed)DataContext).Reseed();
		}
		public event Action CreateLvqModelConfirmed;

		private void Button_Click_1(object sender, RoutedEventArgs e) {
			if (CreateLvqModelConfirmed != null) CreateLvqModelConfirmed();
			ThreadPool.QueueUserWorkItem(o => {
				((CreateLvqModelValues)o).ConfirmCreation();
			},DataContext);

		}
	}
}
