using System;
using System.Xml.Linq;

namespace SongDataLib {
	public static class ISongFileDataMethods {
		public static string UppercaseExtension(this ISongFileData song) { return System.IO.Path.GetExtension(song.SongUri.AbsolutePath).ToUpperInvariant(); }
	}

	public interface ISongFileData {
		/// <summary>
		/// String representation of all meta-data a user is likely to search for.   This will be indexed for searching purposes - i.e it should include certainly the track title,
		/// and perhaps the year released, but certainly not the song length in seconds.
		/// </summary>
		string FullInfo { get; }

		/// <summary>
		/// Converts to xml.  The class should be able to load from xml too then, and supply the appropriate constructor.
		/// </summary>
		/// <returns>A sufficiently complete XML representation such that the object could load from it.</returns>
		XElement ConvertToXml(Func<Uri, string> urlTranslator, bool coreAttrOnly);
		/// <summary>
		/// The path to the song.  This property mixes local path's and remote uri's, to differentiate, use the IsLocal Property.
		/// </summary>
		Uri SongUri { get; }//untranslated, mixes URL's and local filesystem path's willy-nilly!
		/// <summary>
		/// This is a security-sensitive property!
		/// Returns whether this song is a local song.  A local song's SongPath property will potentially be resolved and the song file it points to used.
		/// </summary>
		bool IsLocal { get; }
		/// <summary>
		/// The length of the song in seconds.
		/// </summary>
		int Length { get; }
		/// <summary>
		/// As best as possible, a human-readable version of the meta-data concerning the song.  This is for display in GUI's or so, and thus doesn't need to be as complete as FullInfo.  Must not be null or empty therefore!
		/// This data is a fallback, if possible a user interface should try to use SongData's (or any other implementing class's) more complete data, but if that's to no avail...  
		/// </summary>
		string HumanLabel { get; }
	}
}
