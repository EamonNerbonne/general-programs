using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LvqGui {
	public class LvqWindowValues {
		public AppSettingsValues AppSettingsValues { get; private set; }
		public CreateDataSetValues CreateDataSetValues { get; private set; }
		public CreateDataSetStarValues CreateDataSetStarValues { get; private set; }
		public CreateLvqModelValues CreateLvqModelValues { get; private set; }

		public LvqWindowValues() {
			AppSettingsValues = new AppSettingsValues(this);
			CreateDataSetValues = new CreateDataSetValues(this);
			CreateDataSetStarValues = new CreateDataSetStarValues(this);
			CreateLvqModelValues = new CreateLvqModelValues(this);
		}
	}
}
