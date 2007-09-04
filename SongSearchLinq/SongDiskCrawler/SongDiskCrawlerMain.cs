using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using TagLib;
using System.IO;
using SongDataLib;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Filesystem;

namespace SongDiskCrawlerMain
{

	class Program
	{
		static int counter = 0;
		static int seconds = 0;
		static int newsongs = 0;
		static bool UpdateHandler(string msg)
		{
			counter++;
			if (msg != null) newsongs++;
			if (timer.ElapsedSinceMark.Seconds > seconds) {
				seconds = timer.ElapsedSinceMark.Seconds;
				Console.WriteLine("Nodes/sec: " + (counter / timer.ElapsedSinceMark.TotalSeconds));
				if (msg != null) Console.WriteLine(msg);
			}
			return true;
		}
		static NiceTimer timer;
		static void Main(string[] args)
		{
			timer = new NiceTimer(null);
			timer.TimeMark("Loading Config file " + args[0] + "...");
			DatabaseConfigFile dcf = new DatabaseConfigFile(new FileInfo(args[0]));

			timer.TimeMark("Loading songs from database...");
			counter = 0; seconds = 0;

			dcf.Load(UpdateHandler);
			Console.WriteLine("Found: " + dcf.Songs.Count());

			timer.TimeMark("Rescanning local files...");
			counter = 0; seconds = 0; newsongs = 0;
			dcf.Rescan(UpdateHandler);
			Console.WriteLine("Songs found: " + counter + " of which " + newsongs + " new.");
			timer.TimeMark("Saving songs to database...");
			counter = 0; seconds = 0;
			dcf.Save(UpdateHandler);
			timer.TimeMark(null);
		}
	}
}