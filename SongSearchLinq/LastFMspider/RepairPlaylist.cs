using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EmnExtensions.Algorithms;
using EmnExtensions.Text;
using SongDataLib;

namespace LastFMspider {

	public struct PlaylistSongMatch {
		public static PlaylistSongMatch? Compare(PartialSongFileData orig, SongFileData opt) {
			double lenC = Math.Abs(orig.Length - opt.Length);
			if (lenC > 15) return null;
			string optFileName = RepairPlaylist.NormalizedFileName(Path.GetFileName(opt.SongUri.ToString()));
			string origFilename = RepairPlaylist.NormalizedFileName(orig.SongUri.ToString());
			string optBasicLabel = Canonicalize.Basic(opt.HumanLabel);
			string origBasicLabel = Canonicalize.Basic(orig.HumanLabel);
			double nameCost = origFilename.LevenshteinDistance(optFileName) / (double)(origFilename.Length + optFileName.Length);
			double labelCost = optBasicLabel.LevenshteinDistance(origBasicLabel) / (double)(origBasicLabel.Length + optBasicLabel.Length);
			return new PlaylistSongMatch {
				SongData = opt,
				Orig = orig,
				LenC = lenC,
				NameC = nameCost,
				TagC = labelCost,
				Cost = lenC / 5.0 + Math.Sqrt(50 * Math.Min(nameCost, labelCost)) + Math.Sqrt(50 * labelCost)
			};
		}

		public SongFileData SongData;
		public PartialSongFileData Orig;
		public double Cost;
		double LenC, NameC, TagC;

		public override string ToString() {
			return string.Format("{0,7:g5} {1,7:g5} {2,7:g5} {3,7:g5} {4} ==> {5} ", Cost, LenC, NameC, TagC, ToString(Orig), ToString(SongData));
		}
		public static string ToString(ISongFileData song) {
			return RepairPlaylist.NormalizedFileName(song.SongUri.ToString()) + ": " + song.HumanLabel + " (" + TimeSpan.FromSeconds(song.Length) + ")";
		}
	}


	public static class RepairPlaylist {

		public static ISongFileData[] GetPlaylistFixed(ISongFileData[] playlist, FuzzySongSearcher fuzzySearcher, Func<Uri, SongFileData> findByUri, Action<PartialSongFileData> nomatch, Action<PlaylistSongMatch> toobad, Action<PlaylistSongMatch> iffy, Action<PlaylistSongMatch> matchfound) {
			ISongFileData[] playlistfixed = new ISongFileData[playlist.Length];
			int idx = 0;
			foreach (var songMin in playlist) {
				ISongFileData decentMatch = findByUri(songMin.SongUri);
				if (decentMatch == null) {
					if (songMin is PartialSongFileData) {
						PartialSongFileData song = (PartialSongFileData)songMin;
						decentMatch = FindBestSufficientMatchWithLogging(fuzzySearcher, song, nomatch, toobad, iffy, matchfound);
					} else {
						SongFileData[] exactFilenameMatch = fuzzySearcher.songs.Where(sd => Path.GetFileName(sd.SongUri.ToString()) == Path.GetFileName(songMin.SongUri.ToString()) && Math.Abs(songMin.Length - sd.Length) < 4).ToArray();
						if (exactFilenameMatch.Length == 1)
							decentMatch = exactFilenameMatch[0];
					}
				}
				playlistfixed[idx++] = decentMatch ?? songMin;
			}
			return playlistfixed;
		}
		private static SongFileData FindBestSufficientMatchWithLogging(FuzzySongSearcher fuzzySearcher, PartialSongFileData song, Action<PartialSongFileData> nomatch, Action<PlaylistSongMatch> toobad, Action<PlaylistSongMatch> iffy, Action<PlaylistSongMatch> matchfound) {
			PlaylistSongMatch best = FindBestMatch2(fuzzySearcher, song);
			if (best.SongData == null)
				nomatch(song);
			else if (best.Cost < 6 && SongRef.PossibleSongRefs(song.HumanLabel).Any(songref => songref.Equals(SongRef.Create(best.SongData))))
				matchfound(best);
			else if (best.Cost <= 7.5)
				iffy(best);
			else {
				toobad(best);
				best = new PlaylistSongMatch { SongData = null };
			}

			return best.SongData;
		}

		static PlaylistSongMatch FindBestMatch(FuzzySongSearcher fuzzySearcher, PartialSongFileData songToFind) {
			var q = from songrefOpt in SongRef.PossibleSongRefs(songToFind.HumanLabel)
					from songdataOpt in fuzzySearcher.FindPerfectMatchingSongs(songrefOpt)
					let lengthDiff = Math.Abs(songToFind.Length - songdataOpt.Length)
					where lengthDiff < 9
					let filenameDiff = NormalizedFileName(songToFind.SongUri.ToString()).LevenshteinDistance(NormalizedFileName(songdataOpt.SongUri.ToString()))
					select new PlaylistSongMatch { SongData = songdataOpt, Orig = songToFind, Cost = lengthDiff * 0.5 + filenameDiff * 0.2 };
			return q.Aggregate(new PlaylistSongMatch { SongData = default(SongFileData), Cost = int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b);
		}

		static PlaylistSongMatch FindBestMatch2(FuzzySongSearcher fuzzySearcher, PartialSongFileData songToFind) {//TODO: reimplement with FuzzySongSearcher
			var q =
				from songref in SongRef.PossibleSongRefs(songToFind.HumanLabel)
				from fuzzyMatch in fuzzySearcher.FindMatchingSongs(songref, true)
				let songmatch = PlaylistSongMatch.Compare(songToFind, fuzzyMatch.Song)
				where songmatch.HasValue
				select songmatch.Value;
			// ReSharper disable RedundantCast
			return q.Aggregate(new PlaylistSongMatch { SongData = default(SongFileData), Cost = (double)int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b);
			// ReSharper restore RedundantCast
		}
		static readonly char[] pathSep = { '\\', '/' };
		public static string NormalizedFileName(string origpath) {
			string filename = origpath.Substring(origpath.LastIndexOfAny(pathSep) + 1);
			try {
				return Uri.UnescapeDataString(filename.Replace("100%", "100%25").Replace("%%", "%25%"));
			} catch {//if the not-so-solid uri unescaper can't handle it, assume it's not encoded.  It's no biggy anyhow, this is just normalization.
				return filename;
			}
		}


		public static ISongFileData[] GetPlaylistFixed(ISongFileData[] m3uPlaylist, FuzzySongSearcher fuzzySongSearcher, Func<Uri, SongFileData> lookupSongByUri) {
			return GetPlaylistFixed(m3uPlaylist, fuzzySongSearcher, lookupSongByUri, _ => { }, _ => { }, _ => { }, _ => { });
		}
	}
}
