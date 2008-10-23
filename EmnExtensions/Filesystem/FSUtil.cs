using System.IO;

namespace EmnExtensions.Filesystem
{
	public static class FSUtil
	{
		static char[] invalidChars = Path.GetInvalidPathChars();
		public static bool IsValidPath(string path) {//warning: forward slash is allowed and considered a pathseparator.
			return path.IndexOfAny(invalidChars) < 0;
		}

		/// <summary>
		/// Verifies whether a path is absolute or not.  Returns null if the path is invalid.
		/// </summary>
		/// <param name="path">The Path to test</param>
		/// <returns>null when invalid, false when path is relative, true when path is absolute.</returns>
		public static bool? IsValidAbsolutePath(string path) {
			if(!IsValidPath(path)) return null;
			return Path.IsPathRooted(path);
		}

	}
}
