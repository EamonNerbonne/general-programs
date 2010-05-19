using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using LvqLibCli;
using Microsoft.Win32;
using System.Threading;
using System.IO;
using LVQeamon;

namespace LvqGui {
	public class LoadDataSetValues : INotifyPropertyChanged, IHasSeed {
		readonly LvqWindowValues owner;
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public uint Seed {
			get { return _Seed; }
			set { if (!_Seed.Equals(value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;


		public LoadDataSetValues(LvqWindowValues owner) {
			this.owner = owner;
			this.Reseed();
		}

		LvqDataSetCli CreateDataset() {
			OpenFileDialog dataFileOpenDialog = new OpenFileDialog();
			//dataFileOpenDialog.Filter = "*.data";

			if (dataFileOpenDialog.ShowDialog() == true) {
				FileInfo selectedFile = new FileInfo(dataFileOpenDialog.FileName);
				FileInfo labelFile = new FileInfo(selectedFile.Directory + @"\" + Path.GetFileNameWithoutExtension(selectedFile.Name) + ".label");
				FileInfo dataFile = new FileInfo(selectedFile.Directory + @"\" + Path.GetFileNameWithoutExtension(selectedFile.Name) + ".data");
				if (dataFile.Exists && labelFile.Exists) {
					try {
						var pointclouds = DataSetLoader.LoadDataset(dataFile, labelFile);
						double[,] pointArray = pointclouds.Item1;
						int[] labelArray = pointclouds.Item2;
						int classCount = pointclouds.Item3;

						string name = dataFile.Name + "-" + pointArray.GetLength(1) + "D-" + classCount + "*" + pointArray.GetLength(0);

						return LvqDataSetCli.ConstructFromArray(name, pointArray, labelArray, classCount);


					} catch (FileFormatException fe) {
						Console.WriteLine("Can't load file: {0}", fe.ToString());
						return null;
					}
				} else return null;
			} else return null;
		}

		public void ConfirmCreation() {
			var fileOpenThread = new Thread(() => {
				var dataset = CreateDataset();
				if (dataset != null)
					owner.Dispatcher.BeginInvoke(owner.DataSets.Add, dataset);
			});
			fileOpenThread.SetApartmentState(ApartmentState.STA);
			fileOpenThread.IsBackground = true;
			fileOpenThread.Start();
		}
	}

}
