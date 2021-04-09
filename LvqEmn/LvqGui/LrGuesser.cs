using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EmnExtensions.Algorithms;
using EmnExtensions.Filesystem;
using EmnExtensions.Text;
using LvqGui.CreatorGui;
using LvqLibCli;
using MoreLinq;

namespace LvqGui
{
    public static class LrGuesser
    {
        public static readonly DirectoryInfo resultsDir = FSUtil.FindDataDir(@"uni\Thesis\doc\results\", Assembly.GetAssembly(typeof(LrGuesser)));

        public static LvqModelSettingsCli ChooseReasonableLr(LvqModelSettingsCli settings)
        {
            var options = (
                from tuple in UniformResults()
                let resSettings = tuple.Item1
                let modeltype = resSettings.ModelType
                where modeltype == settings.ModelType || settings.ModelType == LvqModelType.Lpq && resSettings.ModelType == LvqModelType.Lgm
                where settings.PrototypesPerClass == 1 == (resSettings.PrototypesPerClass == 1)
                select resSettings
            ).ToArray();
            var myshorthand = settings.WithCanonicalizedDefaults().ToShorthand();

            if (options.Any()) {
                var bestResults = options.MinBy(resSettings => myshorthand.LevenshteinDistance(resSettings.WithCanonicalizedDefaults().ToShorthand())).First();
                return settings.WithLrAndDecay(bestResults.LR0, bestResults.LrScaleP, bestResults.LrScaleB, bestResults.decay, bestResults.iterScaleFactor)
                    ;
            }

            return settings.ModelType == LvqModelType.Gm
                ? settings.WithLr(0.002, 2.0, 0.0)
                : settings.ModelType == LvqModelType.Ggm
                    ? settings.WithLr(0.03, 0.05, 4.0)
                    : settings.ModelType == LvqModelType.Fgm
                        ? settings.WithLr(0.03, 0.05, 4.0)
                        : settings.WithLr(0.01, 0.4, 0.006);
        }

        public static IEnumerable<Tuple<LvqModelSettingsCli, double>> UniformResults()
            =>
                from line in File.ReadAllLines(resultsDir.FullName + "\\uniform-results.txt")
                let settingsOrNull = CreateLvqModelValues.TryParseShorthand(line.SubstringUntil(" "))
                where settingsOrNull.HasValue
                let settings = settingsOrNull.Value
                let geomean = double.Parse(line.SubstringAfterFirst("GeoMean: ").SubstringUntil(";").SubstringUntil("~"))
                group Tuple.Create(settings, geomean) by settings.WithCanonicalizedDefaults()
                into settingsGroup
                select settingsGroup.MinBy(tuple => tuple.Item2).First();

        static readonly Regex resultsFilenameRegex = new(@"^(?<iters>[0-9]?e[0-9]+)\-(?<shorthand>[^ ]*?)\.txt$");

        public static Tuple<bool, double, LvqModelSettingsCli> ExtractItersAndSettings(string filename)
        {
            var match = resultsFilenameRegex.Match(filename);
            if (!match.Success) {
                return Tuple.Create(false, default(double), default(LvqModelSettingsCli));
            }

            var iters = double.Parse(match.Groups["iters"].Value.StartsWith("e", StringComparison.Ordinal) ? "1" + match.Groups["iters"].Value : match.Groups["iters"].Value);
            var shorthand = match.Groups["shorthand"].Value;
            var modelSettings = CreateLvqModelValues.ParseShorthand(shorthand);
            return Tuple.Create(true, iters, modelSettings);
        }
    }
}
