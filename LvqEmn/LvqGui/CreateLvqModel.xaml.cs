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

		private void ReseedParam(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedParam(); }
		private void ReseedInst(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedInst(); }

		private void InitializeModel(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => ((CreateLvqModelValues)o).ConfirmCreation(), DataContext);
		}
	}
}
