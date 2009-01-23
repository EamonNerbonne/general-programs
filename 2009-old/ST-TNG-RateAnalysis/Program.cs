using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ST_TNG_RateAnalysis
{
    class Program
    {
        const string epregex = @"<td\s+align=""right"">(?<season>\d)\.(?<epNum>\d+)&#160;</td>.*?<td\s+align=""right"" bgcolor=""#eeeeee"">(?<rating>\d\.\d+)</td>";

        public struct Rating
        {
            public int season, epNum;
            public double rating;
            public string series;

        }

        static IEnumerable<Rating> LoadSeries(FileInfo fromFile) {
            string fullfile;
            using(var stream = fromFile.OpenRead())
            using(var reader = new StreamReader(stream,Encoding.GetEncoding(1252)))
                fullfile = reader.ReadToEnd();
            var eps = Regex.Matches(fullfile, epregex, RegexOptions.ExplicitCapture|RegexOptions.Singleline);
            for (int i = 0; i < eps.Count; i++) {

                yield return new Rating {
                    season = int.Parse(eps[i].Groups["season"].Value),
                    epNum = int.Parse(eps[i].Groups["epNum"].Value),
                    rating = double.Parse(eps[i].Groups["rating"].Value),
                    series = fromFile.Name,
                };
            }

        }
        static void Main(string[] args) {


            var q =
                from seriesFiles in new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles("*.htm")
                from episode in LoadSeries(seriesFiles)
                group episode.rating by new { Series = episode.series, Season = episode.season } into seasonsRatings
                let averageRating = seasonsRatings.Average()
                orderby averageRating descending
                select new { W = seasonsRatings.Key.Series.Substring(6), Season = seasonsRatings.Key.Season, Rate = averageRating, EpN = seasonsRatings.Count() };
            foreach (var season in q)
                Console.WriteLine(season.ToString());

        }
    }
}
