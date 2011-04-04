using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SongDataLib {
	public enum SongColumn {
		None, Rating, Artist, Title, Time, TrackNumber, Album
	}

	[Serializable]
	public struct SortOrdering : IEquatable<SortOrdering> {
		readonly int[] colrefs;
		static readonly int[] empty = new int[] { };
		int[] EffectiveCols { get { return colrefs ?? empty; } }
		static readonly char[] splitBy = new[] { ',', ' ' };
		public override int GetHashCode() {
			int code = 0;
			foreach (int val in colrefs) { code = code * 37 + val; }
			return code;
		}
		SortOrdering(int[] cols) { colrefs = cols; }
		static SortOrdering FromRawSeq(IEnumerable<int> cols) { return new SortOrdering(cols.ToArray()); }
		public static SortOrdering Empty { get { return default(SortOrdering); } }
		public static SortOrdering FromSeq(IEnumerable<int> cols) { var set = new HashSet<int>(); return new SortOrdering(cols.Where(i => i != 0 && set.Add(Math.Abs(i))).ToArray()); }
		public static SortOrdering FromString(string rep) {
			return FromSeq((rep ?? "").Split(splitBy, StringSplitOptions.RemoveEmptyEntries).Select(ParseOneOrderCol));
		}

		public static SortOrdering Parse(string rep) {
			if (rep != null) {
				string[] parts = rep.Split(':');
				if (parts.Length == 2) {
					SongColumn toggleCol;
					return FromString(parts[1]).Toggle(Enum.TryParse(parts[0], out toggleCol) ? toggleCol : SongColumn.None);
				} else
					return FromString(rep);
			} else return Empty;
		}


		public static int ParseOneOrderCol(string colname) {
			colname = colname.Trim();
			SongColumn column;
			if (Enum.TryParse(colname.Substring(1), out column))
				return (int)column * (colname.StartsWith("+") ? 1 : -1);
			else
				return (int)SongColumn.None;
		}
		public override string ToString() { return string.Join(",", EffectiveCols.Select(i => (i > 0 ? "+" : "-") + (SongColumn)Math.Abs(i)).ToArray()); }


		public override bool Equals(object obj) { return obj is SortOrdering && Equals((SortOrdering)obj); }
		public bool Equals(SortOrdering other) { return other.EffectiveCols.SequenceEqual(EffectiveCols); }
		public static bool operator ==(SortOrdering a, SortOrdering b) { return a.Equals(b); }
		public static bool operator !=(SortOrdering a, SortOrdering b) { return !a.Equals(b); }

		IEnumerable<int> Nonconflicting(int col) { return EffectiveCols.Where(existing => Math.Abs(existing) != Math.Abs(col)); }
		bool IsConflicting(int col) { return EffectiveCols.Any(existing => Math.Abs(existing) == Math.Abs(col)); }
		int CurrentDirection(int col) { return Math.Sign(EffectiveCols.Where(existing => Math.Abs(existing) == Math.Abs(col)).FirstOrDefault()); }

		public SortOrdering Prepend(int col) { return col == 0 ? this : FromRawSeq(Enumerable.Repeat(col, 1).Concat(Nonconflicting(col))); }

		public SortOrdering Append(int col) { return IsConflicting(col) || col == 0 ? this : new SortOrdering(EffectiveCols.Concat(Enumerable.Repeat(col, 1)).ToArray()); }
		public SortOrdering Toggle(SongColumn col) {
			if (Math.Abs(EffectiveCols.FirstOrDefault()) == (int)col)
				return FromRawSeq(Enumerable.Repeat(-EffectiveCols.FirstOrDefault(), 1).Concat(EffectiveCols.Skip(1)));
			else
				return Prepend(Math.Sign(CurrentDirection((int)col) * 2 + 1) * (int)col);
		}
		public IEnumerable<Tuple<SongColumn, bool>> Order { get { return colrefs.Select(def => Tuple.Create((SongColumn)Math.Abs(def), def > 0)); } }

		public XElement ToXml() {
			return
				new XElement("ordering",
					EffectiveCols.Select(i =>
						new XElement("col", 
							new XAttribute("name", ((SongColumn)Math.Abs(i)).ToString()),
							new XAttribute("dir", i < 0 ? "desc" : "asc")
						)
					)
				);
		}
	}

	public static class SortOrder {

		static IEnumerable<TVal> OrderByEither<TVal, TKey>(this IEnumerable<TVal> indexes, Func<TVal, TKey> keySelector, bool isAscending) {
			var ordered = indexes as IOrderedEnumerable<TVal>;
			return ordered == null
					? (isAscending ? indexes.OrderBy(keySelector) : indexes.OrderByDescending(keySelector))
					: (isAscending ? ordered.ThenBy(keySelector) : ordered.ThenByDescending(keySelector));
		}

		static IEnumerable<int> StringOrder(this IEnumerable<int> indexes, Func<int, string> keySelector, bool isAscending) {
			return indexes.OrderByEither(i => string.IsNullOrEmpty(keySelector(i)), isAscending).OrderByEither(keySelector, isAscending);
		}

		public static int[] RankMapFor(ISongFileData[] songs, SortOrdering ordering) {
			var indexes = Enumerable.Range(0, songs.Length);
			foreach (var column in ordering.Order) {
				switch (column.Item1) {
					case SongColumn.Album: indexes = indexes.StringOrder(i => !(songs[i] is SongFileData) ? null : ((SongFileData)songs[i]).album, column.Item2); break;
					case SongColumn.Artist: indexes = indexes.StringOrder(i => !(songs[i] is SongFileData) ? null : ((SongFileData)songs[i]).artist, column.Item2); break;
					case SongColumn.Rating: indexes = indexes.OrderByEither(i => ((!(songs[i] is SongFileData) ? null : ((SongFileData)songs[i]).rating) ?? 2.5) * (!(songs[i] is SongFileData) ? 0 : ((SongFileData)songs[i]).popularity.TitlePopularity), !column.Item2);
						break;
					case SongColumn.Time: indexes = indexes.OrderByEither(i => !(songs[i] is SongFileData) ? 0 : ((SongFileData)songs[i]).length, column.Item2); break;
					case SongColumn.Title: indexes = indexes.StringOrder(i => !(songs[i] is SongFileData) ? null : ((SongFileData)songs[i]).title, column.Item2); break;
					case SongColumn.TrackNumber: indexes = indexes.OrderByEither(i => !(songs[i] is SongFileData) ? 0 : ((SongFileData)songs[i]).track, column.Item2); break;
					default: throw new ArgumentOutOfRangeException("ordering");
				}
			}
			int[] rankmap = new int[songs.Length];
			int currRank = 0;
			foreach (int index in indexes)
				rankmap[index] = currRank++;

			return rankmap;
		}
	}
}
