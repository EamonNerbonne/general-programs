using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuralNetworks
{
	public struct TrainingSettings
	{
		public int MaxEpoch;
		public int TrialRuns;
		public bool UseCenterOfMass;
		public int N;
		public int P;

		bool IsDefined { get { return MaxEpoch * TrialRuns * N * P != 0; } }

		public IEnumerable<TrainingSettings> SettingsWithReasonableP {
			get {
				double ComputeExtent = Math.Sqrt(30.0 * N);
				int stepSize = Math.Max((int)(ComputeExtent / 10), 1);
				for(int p =stepSize;p <2*N+ComputeExtent;p+=stepSize) {
					var copy = this;
					copy.P = p;
					yield return copy;
				}
			}
		}
	}
}
