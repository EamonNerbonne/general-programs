﻿using System;
using System.Xml.Linq;
using EmnExtensions.Text;

namespace SongDataLib
{
	class RemoteSongDataConfigSection : AbstractSongDataConfigSection
	{
		public string href, login, pass;
		public RemoteSongDataConfigSection(XElement xEl, SongDataConfigFile dcf)
			: base(xEl, dcf) {
			href = (string)xEl.Attribute("href");
			login = (string)xEl.Attribute("login");
			pass = (string)xEl.Attribute("pass");
			if(name.IsNullOrEmpty() || href.IsNullOrEmpty()) throw new Exception("Missing attributes for remoteDB");
		}


		protected override void ScanSongs(FileKnownFilter filter, SongDataLoadDelegate handler, Action<string> errSink) {
			if(dbFile.LastWriteTimeUtc.AddDays(7) > DateTime.UtcNow)
				return;//TODO: note that this means that SongDataLoadDelegate isn't called for each song, which might break application assumptions.
			try {
				SongFileDataFactory.LoadSongsFromPathOrUrl(href, delegate(ISongFileData newsong, double estimatedCompletion) {
					handler(newsong, estimatedCompletion);
				}, false,login,pass,dcf.PopularityEstimator);
			} catch(Exception e) {
				errSink("Exception while scanning:\n"+e.ToString());
			}
		}
		protected override bool IsLocal { get { return false; } }
	}
}