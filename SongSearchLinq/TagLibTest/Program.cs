using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TagLib;
using System.Windows;
using EmnExtensions.Wpf;
using System.Threading;

namespace TagLibTest
{
	class Program
	{
		[STAThread]
		static void Main(string[] args) {
			bool loaded = false;
			Application app = new Application();
			using (Semaphore sem = new Semaphore(0, 2)) {
				Window w =
					new Window {
						Content = new LogControl {
							ClaimStandardError = true,
							ClaimStandardOut = true,
						},
					};
				w.Loaded += (o, e) => { if (!loaded) { loaded = true; sem.Release(); } };
				w.Show();
				sem.WaitOne();
				File file = File.Create(args[0]);
				foreach (var pi in file.Tag.GetType().GetProperties()) {
					try {
						object val = pi.GetValue(file.Tag, new object[] { });
						if (val == null || "".Equals(val) || (val is IEnumerable<object> && (val as IEnumerable<object>).Count()==0 ) )
							continue;
						if (val is IEnumerable<object>) {
							var list = (IEnumerable<object>)val;
							if (list.Count() == 0)
								continue;
							else
								val = "List["+list.Count()+ "]{ " + string.Join(", ", list.Select(el => el == null ? "<null>" : el.ToString()).ToArray()) + " }";
						}

						Console.WriteLine("{0}: {1}", pi.Name, val);
					} catch (Exception e) {
						Console.WriteLine("{0}:(!) {1}", pi.Name, e.GetType().FullName);
					}

					//Console.Write("Artist: {0}\nTitle:{1}\nAlbum:{2}\nYear:{3}\nTrack:{4}\nTrackCount:{5}\nDisc:{6}\n", file.Tag.JoinedPerformers, file.Tag.Title, file.Tag.Album, file.Tag.Year, file.Tag.Track, file.Tag.TrackCount, file.Tag.Disc);
				}

			}
			app.Run();
		}

	}
}
