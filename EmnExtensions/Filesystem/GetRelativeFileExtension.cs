using System.IO;
namespace EamonExtensionsLinq.Filesystem
{
	public static class GetRelativeFileExtension
	{
		public static FileInfo GetRelativeFile(this DirectoryInfo dir, string relativepath) {
			return new FileInfo(Path.Combine(dir.FullName, relativepath));
		}
	}
}
