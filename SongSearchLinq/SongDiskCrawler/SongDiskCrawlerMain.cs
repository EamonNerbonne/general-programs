﻿using System;
using System.Collections.Generic;
using EmnExtensions;
using SongDataLib;
using EmnExtensions.DebugTools;

namespace SongDiskCrawlerMain
{

	class Program
	{
		static int counter = 0;
		static int newsongs = 0;
		static double lastmark = 0;
		static int lastcounter = 0;
		static Dictionary<string, ISongData> songs = new Dictionary<string, ISongData>();
		static void SongHandler(ISongData song, double ratio) {
			songs[song.SongPath] = song;
			counter++;
			double seconds = (timer.ElapsedSinceMark.TotalSeconds - lastmark);
			if(seconds > 10.0) {
				lastmark = timer.ElapsedSinceMark.TotalSeconds;
				Console.WriteLine("{0:f1}%, at {1:f1} nodes/sec: ", ratio * 100, (counter - lastcounter) / seconds);
				lastcounter = counter;
			}
		}
		static ISongData LookupSong(string url) {
			ISongData retval=null;
			songs.TryGetValue(url, out retval);
			if(retval == null) newsongs++;
			return retval;
		}
		static NiceTimer timer;
		static void Main(string[] args) {

			TagLib.File test;
			string title;
			test = TagLib.File.Create(@"E:\Music\2004-03-08\10 DOUBLE.mp3");
			title = test.Tag.Title;
			test = TagLib.File.Create(@"E:\Music\2004-03-08\BoA - Rock with you.mp3");
			title = test.Tag.Title;
			test = TagLib.File.Create(@"E:\Music\2003-05-01\Donovan - Essence To Essence - 01 - Operating Maunal For Spaceship Earth.mp3");
			title = test.Tag.Title;

#if !DEBUG
			try {
#endif
				timer = new NiceTimer();
				timer.TimeMark("Loading Config file...");
				SongDatabaseConfigFile dcf = new SongDatabaseConfigFile(true);

				timer.TimeMark("Loading songs from database...");

				dcf.Load(SongHandler);
				Console.WriteLine("Found: " + songs.Count);
				lastcounter = counter = 0;
				timer.TimeMark("Rescanning files...");
				dcf.RescanAndSave(LookupSong, SongHandler);
				Console.WriteLine("Songs found: " + counter + " of which " + newsongs + " new.");
				timer.TimeMark(null);
#if !DEBUG
			} catch(Exception e) {
				Console.WriteLine("==========================");
				Console.WriteLine("===TERMINAL ERROR=========");
				Console.WriteLine("==========================");
				Console.WriteLine(e.ToString());
				Console.WriteLine("Press any key to ABORT...");
				Console.ReadKey();
				throw;
			}
#endif
		}
	}
}