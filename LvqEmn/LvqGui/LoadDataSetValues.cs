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

		public uint Seed {
			get { return _Seed; }
			set { if (!Equals(_Seed,value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		uint _Seed;

		public uint InstSeed {
			get { return _InstSeed; }
			set { if (!_InstSeed.Equals(value)) { _InstSeed = value; _propertyChanged("InstSeed"); } }
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
			this.ReseedBoth();
		}

		LvqDatasetCli CreateDataset(uint seed, int folds) {
			var dataFileOpenDialog = new OpenFileDialog();
			using (var lvqGuiKey = Registry.CurrentUser.OpenSubKey(@"Software\LvqGui")) 
				if(lvqGuiKey!=null)
					dataFileOpenDialog.InitialDirectory = lvqGuiKey.GetValue("DataDir") as string;
			

			//dataFileOpenDialog.Filter = "*.data";

			if (dataFileOpenDialog.ShowDialog() ?? false) {
				var selectedFile = new FileInfo(dataFileOpenDialog.FileName);
				using (var lvqGuiKey = Registry.CurrentUser.CreateSubKey(@"Software\LvqGui")) {
					lvqGuiKey.SetValue("DataDir", selectedFile.Directory.FullName);
				}

				var labelFile = new FileInfo(selectedFile.Directory + @"\" + Path.GetFileNameWithoutExtension(selectedFile.Name) + ".label");
				var dataFile = new FileInfo(selectedFile.Directory + @"\" + Path.GetFileNameWithoutExtension(selectedFile.Name) + ".data");
				if (dataFile.Exists && labelFile.Exists) {
					try {
						var pointclouds = DatasetLoader.LoadDataset(dataFile, labelFile);
						double[,] pointArray = pointclouds.Item1;
						int[] labelArray = pointclouds.Item2;
						int classCount = pointclouds.Item3;
						long colorSeedLong = labelArray.Select((label, i) => label * (long)(i + 1)).Sum();
						int colorSeed = (int) (colorSeedLong + (colorSeedLong >> 32));


						string name = dataFile.Name + "-" + pointArray.GetLength(1) + "D" + (owner.ExtendDataByCorrelation ? "*" : "") + "-" + classCount + ":" + pointArray.GetLength(0) + "[" + seed + "]/" + folds;
						Console.WriteLine("Created: " + name);
						return LvqDatasetCli.ConstructFromArray(
							rngInstSeed:seed,
							label:name,
							extend: owner.ExtendDataByCorrelation,
							folds:folds,
							colors: WpfTools.MakeDistributedColors(classCount,new MersenneTwister(colorSeed)), 
							points: pointArray, 
							pointLabels: labelArray, 
							classCount: classCount);


					} catch (FileFormatException fe) {
						Console.WriteLine("Can't load file: {0}", fe);
						return null;
					}
				} else return null;
			} else return null;
		}

		public void ConfirmCreation() {
			var fileOpenThread = new Thread(o => {
				var t = (Tuple<uint, int>)o;
				var dataset = CreateDataset(t.Item1,t.Item2);
				if (dataset != null)
					owner.Dispatcher.BeginInvoke(owner.Datasets.Add, dataset);
			});
			fileOpenThread.SetApartmentState(ApartmentState.STA);
			fileOpenThread.IsBackground = true;
			fileOpenThread.Start(Tuple.Create(_InstSeed, _Folds));
		}
	}
}
