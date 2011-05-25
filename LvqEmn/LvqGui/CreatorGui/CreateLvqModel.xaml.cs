using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using LvqLibCli;

namespace LvqGui {

	public partial class CreateLvqModel {
		// ReSharper disable MemberCanBeMadeStatic.Global
		// ReSharper disable MemberCanBePrivate.Global
		public IEnumerable<LvqModelType> ModelTypes { get { return (LvqModelType[])Enum.GetValues(typeof(LvqModelType)); } }
		// ReSharper restore MemberCanBePrivate.Global
		// ReSharper restore MemberCanBeMadeStatic.Global
		public CreateLvqModel() { InitializeComponent(); }

		void ReseedParam(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedParam(); }
		void ReseedInst(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedInst(); }

		void InitializeModel(object sender, RoutedEventArgs e) { ((CreateLvqModelValues)DataContext).ConfirmCreation(); }

		void OptimizeLr(object sender, RoutedEventArgs e) { ((CreateLvqModelValues)DataContext).OptimizeLr(); }
		void OptimizeLrAll(object sender, RoutedEventArgs e) { ((CreateLvqModelValues)DataContext).OptimizeLrAll(); }
	}
}
