using System.IO;
using System;
using System.Linq;
using System.Reflection;

namespace EmnExtensions.Filesystem
{
    public static class FSUtil
    {
        static char[] invalidChars = Path.GetInvalidPathChars();
        public static bool IsValidPath(string path) {//warning: forward slash is allowed and considered a pathseparator.
            return path.IndexOfAny(invalidChars) < 0;
        }

        /// <summary>
        /// Verifies whether a path is valid and absolute or not.  Returns false if the path is either invalid or not absolute.
        /// </summary>
        public static bool IsValidAbsolutePath(string path) {
            Uri pathUri;
            if (!Uri.TryCreate(path, UriKind.Absolute, out pathUri))
                return false;
            return pathUri.IsFile;
        }

        public static DirectoryInfo FindDataDir(string relpath, Type relativeToAssemblyFor) {
            return FindDataDir(relpath, Assembly.GetAssembly(relativeToAssemblyFor));
        }

        public static DirectoryInfo FindDataDir(string relpath, Assembly relativeTo=null) {
            return new FileInfo((relativeTo??Assembly.GetCallingAssembly()).Location)
                .Directory.ParentDirs()
                .Select(dir => Path.Combine(dir.FullName + @"\", relpath))
                .Where(Directory.Exists)
                .Select(path => new DirectoryInfo(path))
                .FirstOrDefault();
        }
        public static DirectoryInfo FindDataDir(string[] relpaths, Type relativeToAssemblyFor) {
            return FindDataDir(relpaths, Assembly.GetAssembly(relativeToAssemblyFor));
        }

        public static DirectoryInfo FindDataDir(string[] relpaths, Assembly relativeTo = null) {
            return new FileInfo((relativeTo ?? Assembly.GetCallingAssembly()).Location)
                .Directory.ParentDirs()
                .SelectMany(dir => relpaths.Select(relpath=>Path.Combine(dir.FullName + @"\", relpath)))
                .Where(Directory.Exists)
                .Select(path => new DirectoryInfo(path))
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns the path of the tgtPath parameter relative to the basePath parameter.  Both parameters must be absolute paths.  
        /// If either represents a directory, it should end with a directory seperating character (e.g. a backslash)
        /// </summary>
        public static string MakeRelativePath(string basePath, string tgtPath) {
            return
                Uri.UnescapeDataString(
                    new Uri(basePath, UriKind.Absolute).MakeRelativeUri(new Uri(tgtPath, UriKind.Absolute)).ToString()
                ).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
