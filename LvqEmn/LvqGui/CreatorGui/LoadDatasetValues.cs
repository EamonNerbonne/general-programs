// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using EmnExtensions.Wpf;
using System.Linq;
using LvqLibCli;
using Microsoft.Win32;
using EmnExtensions.MathHelpers;

namespace LvqGui {
	public class LoadDatasetValues : INotifyPropertyChanged, IHasSeed {
		readonly LvqWindowValues owner;
		public event PropertyChangedEventHandler PropertyChanged;
		void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public uint ParamsSeed {
			get { return _Seed; }
			set { if (!Equals(_Seed, value)) { _Seed = value; _propertyChanged("ParamsSeed"); } }
		}
		uint _Seed;

		public uint InstanceSeed {
			get { return _InstSeed; }
			set { if (!_InstSeed.Equals(value)) { _InstSeed = value; _propertyChanged("InstanceSeed"); } }
		}
		uint _InstSeed;

		public int Folds {
			get { return _Folds; }
			set { if (value != 0 && value < 2) throw new ArgumentException("Must have no folds (no test data) or at least 2"); if (!_Folds.Equals(value)) { _Folds = value; _propertyChanged("Folds"); } }
		}
		int _Folds;


		public LoadDatasetValues(LvqWindowValues owner) {
			this.owner = owner;
			_Folds = 10;
			//this.ReseedBoth();
		}

		LvqDatasetCli CreateDataset(uint seed, int folds) {
			var dataFileOpenDialog = new OpenFileDialog { Multiselect = folds == 0, Filter = "Dataset|*.data" };
			using (var lvqGuiKey = Registry.CurrentUser.OpenSubKey(@"Software\LvqGui"))
				if (lvqGuiKey != null)
					dataFileOpenDialog.InitialDirectory = lvqGuiKey.GetValue("DataDir") as string;

			if (dataFileOpenDialog.ShowDialog() ?? false) {
				var selectedFile = new FileInfo(dataFileOpenDialog.FileName);
				using (var lvqGuiKey = Registry.CurrentUser.CreateSubKey(@"Software\LvqGui")) {
					lvqGuiKey.SetValue("DataDir", selectedFile.Directory.FullName);
				}

				if (dataFileOpenDialog.FileNames.Length == 1) {
					try {
						return LoadDataset(selectedFile, seed, folds);
					} catch (FileFormatException fe) {
						Console.WriteLine("Can't load file: {0}", fe);
						return null;
					}
				} else if (dataFileOpenDialog.FileNames.Length == 2) {
					var trainFile = dataFileOpenDialog.FileNames.Where(name => Path.GetFileNameWithoutExtension(name).EndsWith("train")).Select(name => new FileInfo(name)).FirstOrDefault();
					var testFile = dataFileOpenDialog.FileNames.Where(name => Path.GetFileNameWithoutExtension(name).EndsWith("test")).Select(name => new FileInfo(name)).FirstOrDefault();
					if (trainFile == null || testFile == null)
						return null;
					try {
						var dataset = LoadDataset(trainFile, seed, folds, testFile.Name);
						dataset.TestSet = LoadDataset(testFile, seed, folds);
						return dataset;
					} catch (FileFormatException fe) {
						Console.WriteLine("Can't load file: {0}", fe);
						return null;
					}
				} else return null;
			} else return null;
		}

		LvqDatasetCli LoadDataset(FileInfo dataFile, uint seed, int folds, string testFile = null) {
			return LoadDatasetImpl.LoadData(dataFile, new LoadDatasetImpl.LoadedDatasetSettings { TestFilename = testFile, Extend = owner.ExtendDataByCorrelation, Normalize = owner.NormalizeDimensions, InstanceSeed = seed, Folds = folds });
		}

		public void ConfirmCreation() {
			var fileOpenThread = new Thread(o => {
				var t = (Tuple<uint, int>)o;
				var dataset = CreateDataset(t.Item1, t.Item2);
				if (dataset != null)
					owner.Dispatcher.BeginInvoke(owner.Datasets.Add, dataset);
			});
			fileOpenThread.SetApartmentState(ApartmentState.STA);
			fileOpenThread.IsBackground = true;
			fileOpenThread.Start(Tuple.Create(_InstSeed, _Folds));
		}
	}
}
