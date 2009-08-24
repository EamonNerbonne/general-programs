using System.IO;
using System.Linq;

namespace HwrDataModel {
    public static class Program {

		public readonly static string[] DataPaths = new[] { @"D:\EamonLargeDocs\HWR\data", @"C:\Users\nerbonne\HWR\data" };
		public static string DataPath { get { return DataPaths.First(path => Directory.Exists(path)); } }
		public readonly static string[] ImgPaths = new[] { @"D:\EamonLargeDocs\HWR\Original", @"C:\Users\nerbonne\HWR\Original" };
		public static string ImgPath { get { return ImgPaths.First(path => Directory.Exists(path)); } }
    }
}
