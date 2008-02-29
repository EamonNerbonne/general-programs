using System;
using System.Xml.Linq;
using EamonExtensionsLinq.Text;

namespace SongDataLib
{
	class RemoteSongDatabaseSection : AbstractSongDatabaseSection
	{
		public string href, login, pass;
		public RemoteSongDatabaseSection(XElement xEl, SongDatabaseConfigFile dcf)
			: base(xEl, dcf) {
			href = (string)xEl.Attribute("href");
			login = (string)xEl.Attribute("login");
			pass = (string)xEl.Attribute("pass");
			if(name.IsNullOrEmpty() || href.IsNullOrEmpty()) throw new Exception("Missing attributes for remoteDB");
		}


		protected override void ScanSongs(FileKnownFilter filter, SongDataLoadDelegate handler) {
			if(dbFile.LastWriteTime.AddDays(7) > DateTime.Now)
				return;//TODO: note that this means that SongDataLoadDelegate isn't called for each song, which might break application assumptions.
			try {
				SongDataFactory.LoadSongsFromPathOrUrl(href, delegate(ISongData newsong, double estimatedCompletion) {
					handler(newsong, estimatedCompletion);
				}, false,login,pass);
			} catch(Exception e) {
				Console.WriteLine("Exception while scanning:");
				Console.WriteLine(e.ToString());
			}
		}
		protected override bool IsLocal { get { return false; } }
	}
}
