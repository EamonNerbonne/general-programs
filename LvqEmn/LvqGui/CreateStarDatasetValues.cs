﻿// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqLibCli;

namespace LvqGui {

	public class CreateStarDatasetValues : INotifyPropertyChanged, IHasSeed, IHasShorthand {
		readonly LvqWindowValues owner;

		public event PropertyChangedEventHandler PropertyChanged;
		void raisePropertyChanged(string prop) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }

		private void _propertyChanged(String propertyName) {
			if (PropertyChanged != null) {
				raisePropertyChanged(propertyName);
				raisePropertyChanged("Shorthand");
				raisePropertyChanged("ShorthandErrors");
			}
		}

		public int Dimensions {
			get { return _Dimensions; }
			set { if (value < _ClusterDimensionality) throw new ArgumentException("Data needs at least one dimension and no fewer than the clusters' dimensions"); if (!Equals(_Dimensions, value)) { _Dimensions = value; _propertyChanged("Dimensions"); } }
		}
		private int _Dimensions;

		public int NumberOfClasses {
			get { return _NumberOfClasses; }
			set { if (value < 2) throw new ArgumentException("Need at least 2 classes to meaningfully train"); if (!Equals(_NumberOfClasses, value)) { _NumberOfClasses = value; _propertyChanged("NumberOfClasses"); } }
		}
		private int _NumberOfClasses;

		public int PointsPerClass {
			get { return _PointsPerClass; }
			set { if (value < 1) throw new ArgumentException("Need a positive number of points"); if (!Equals(_PointsPerClass, value)) { _PointsPerClass = value; _propertyChanged("PointsPerClass"); } }
		}
		private int _PointsPerClass;

		public int NumberOfClusters {
			get { return _NumberOfClusters; }
			set { if (value < 1) throw new ArgumentException("Need a positive number of clusters"); if (!Equals(_NumberOfClusters, value)) { _NumberOfClusters = value; _propertyChanged("NumberOfClusters"); } }
		}
		private int _NumberOfClusters;

		public int ClusterDimensionality {
			get { return _ClusterDimensionality; }
			set { if (value < 1 || value > _Dimensions) throw new ArgumentException("Cluster dimensionality must be a positive number less than the absolute dimensionality"); if (!Equals(_ClusterDimensionality, value)) { _ClusterDimensionality = value; _propertyChanged("ClusterDimensionality"); } }
		}
		private int _ClusterDimensionality;

		public bool RandomlyTransformFirst {
			get { return _RandomlyTransformFirst; }
			set { if (!Equals(_RandomlyTransformFirst, value)) { _RandomlyTransformFirst = value; _propertyChanged("RandomlyTransformFirst"); } }
		}
		private bool _RandomlyTransformFirst;

		public double ClusterCenterDeviation {
			get { return _ClusterCenterDeviation; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!Equals(_ClusterCenterDeviation, value)) { _ClusterCenterDeviation = value; _propertyChanged("ClusterCenterDeviation"); } }
		}
		private double _ClusterCenterDeviation;

		public double IntraClusterClassRelDev {
			get { return _IntraClusterClassRelDev; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!Equals(_IntraClusterClassRelDev, value)) { _IntraClusterClassRelDev = value; _propertyChanged("IntraClusterClassRelDev"); } }
		}
		private double _IntraClusterClassRelDev;

		public uint Seed {
			get { return _Seed; }
			set { if (!Equals(_Seed, value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;

		public uint InstSeed {
			get { return _InstSeed; }
			set { if (!_InstSeed.Equals(value)) { _InstSeed = value; _propertyChanged("InstSeed"); } }
		}
		private uint _InstSeed;

		public int Folds {
			get { return _Folds; }
			set { if (value != 0 && value < 2) throw new ArgumentException("Must have no folds (no test data) or at least 2"); if (!_Folds.Equals(value)) { _Folds = value; _propertyChanged("Folds"); } }
		}
		private int _Folds;

		public bool ExtendDataByCorrelation { get { return owner.ExtendDataByCorrelation; } set { owner.ExtendDataByCorrelation = value; } }

		static readonly Regex shR =
			new Regex(@"^\s*(.*--)?star-(?<Dimensions>\d+)D(?<ExtendDataByCorrelation>\*?)-(?<NumberOfClasses>\d+)\*(?<PointsPerClass>\d+):(?<NumberOfClusters>\d+)\((?<ClusterDimensionality>\d+)D(?<RandomlyTransformFirst>\??)\)\*(?<ClusterCenterDeviation>[^~]+)\~(?<IntraClusterClassRelDev>[^\[]+)\[(?<Seed>\d+):(?<InstSeed>\d+)\]/(?<Folds>\d+)\s*$",
				RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		public string Shorthand {
			get {
				return "star-" + Dimensions + "D" + (ExtendDataByCorrelation ? "*" : "") + "-" + NumberOfClasses + "*" + PointsPerClass + ":" + NumberOfClusters + "(" + ClusterDimensionality + "D" + (RandomlyTransformFirst ? "?" : "") + ")*" + ClusterCenterDeviation.ToString("r") + "~" + IntraClusterClassRelDev.ToString("r") + "[" + Seed + ":" + InstSeed + "]/" + Folds;
			}
			set { ShorthandHelper.ParseShorthand(this, shR, value); }
		}

		public string ShorthandErrors { get { return ShorthandHelper.VerifyShorthand(this, shR); } }

		public CreateStarDatasetValues(LvqWindowValues owner) {
			this.owner = owner;
			owner.PropertyChanged += (o, e) => { if (e.PropertyName == "ExtendDataByCorrelation") _propertyChanged("ExtendDataByCorrelation"); };
			_Folds = 10;
			_ClusterCenterDeviation = 3.5;
			_ClusterDimensionality = 4;
			_IntraClusterClassRelDev = 0.4;
			_NumberOfClasses = 3;
			_NumberOfClusters = 4;
#if DEBUG
			_Dimensions = 8;
			_PointsPerClass = 100;
#else
			_Dimensions = 24;
			_PointsPerClass = 1000;
#endif
			_RandomlyTransformFirst = true;
			this.ReseedBoth();
		}


		public LvqDatasetCli CreateDataset() {
			Console.WriteLine("Created: " + Shorthand);
			return LvqDatasetCli.ConstructStarDataset(Shorthand,
				colors: WpfTools.MakeDistributedColors(NumberOfClasses, new MersenneTwister((int)Seed)),
				folds: _Folds,
				extend: owner.ExtendDataByCorrelation,
				rngParamsSeed: Seed,
				rngInstSeed: InstSeed,
				dims: Dimensions,
				starDims: ClusterDimensionality,
				numStarTails: NumberOfClusters,
				classCount: NumberOfClasses,
				pointsPerClass: PointsPerClass,
				starMeanSep: ClusterCenterDeviation,
				starClassRelOffset: IntraClusterClassRelDev,
				randomlyTransform: RandomlyTransformFirst
				);
		}

		public void ConfirmCreation() {
			owner.Dispatcher.BeginInvoke(owner.Datasets.Add, CreateDataset());
		}
	}
}