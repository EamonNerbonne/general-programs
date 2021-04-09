using System;

namespace LvqGui
{
    class LvqStatName {
        public readonly string TrainingStatLabel, UnitLabel, StatGroup;
        public readonly bool HideByDefault;
        public readonly int Index;

        LvqStatName(string compoundName, int index) {
            if (index < 0) throw new ArgumentException("index must be positive");
            Index = index;
            string[] splitName = compoundName.Split('!');
            if (splitName.Length < 2) throw new ArgumentException("compound name has too few components");
            if (splitName.Length > 3) throw new ArgumentException("compound name has too many components");
            TrainingStatLabel = splitName[0];
            UnitLabel = splitName[1];
            StatGroup = splitName.Length > 2 ? splitName[2] : null;
            HideByDefault = StatGroup != null && StatGroup.StartsWith("$");
            if (HideByDefault) StatGroup = StatGroup.Substring(1);

        }
        public static LvqStatName Create(string compoundName, int index) { return new LvqStatName(compoundName, index); }
    }
}