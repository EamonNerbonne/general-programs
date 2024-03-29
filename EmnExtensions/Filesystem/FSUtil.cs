using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EmnExtensions.Filesystem
{
    public static class FSUtil
    {
        static readonly char[] invalidChars = Path.GetInvalidPathChars();

        public static bool IsValidPath(string path)
            => path.IndexOfAny(invalidChars) < 0;

        /// <summary>
        /// Verifies whether a path is valid and absolute or not.  Returns false if the path is either invalid or not absolute.
        /// </summary>
        public static bool IsValidAbsolutePath(string path)
        {
            if (!Uri.TryCreate(path, UriKind.Absolute, out var pathUri)) {
                return false;
            }

            return pathUri.IsFile;
        }

        public static DirectoryInfo FindDataDir(string relpath, Type relativeToAssemblyFor)
            => FindDataDir(relpath, Assembly.GetAssembly(relativeToAssemblyFor));

        public static DirectoryInfo FindDataDir(string relpath, Assembly relativeTo = null)
            => new FileInfo((relativeTo ?? Assembly.GetCallingAssembly()).Location)
                .Directory.ParentDirs()
                .Select(dir => Path.Combine(dir.FullName + @"\", relpath))
                .Where(Directory.Exists)
                .Select(path => new DirectoryInfo(path))
                .FirstOrDefault();

        public static DirectoryInfo FindDataDir(string[] relpaths, Type relativeToAssemblyFor)
            => FindDataDir(relpaths, Assembly.GetAssembly(relativeToAssemblyFor));

        public static DirectoryInfo FindDataDir(string[] relpaths, Assembly relativeTo = null)
            => new FileInfo((relativeTo ?? Assembly.GetCallingAssembly()).Location)
                .Directory.ParentDirs()
                .SelectMany(dir => relpaths.Select(relpath => Path.Combine(dir.FullName + @"\", relpath)))
                .Where(Directory.Exists)
                .Select(path => new DirectoryInfo(path))
                .FirstOrDefault();

        /// <summary>
        /// Returns the path of the tgtPath parameter relative to the basePath parameter.  Both parameters must be absolute paths.
        /// If either represents a directory, it should end with a directory seperating character (e.g. a backslash)
        /// </summary>
        public static string MakeRelativePath(string basePath, string tgtPath)
            => Uri.UnescapeDataString(new Uri(basePath, UriKind.Absolute).MakeRelativeUri(new(tgtPath, UriKind.Absolute)).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }
}
