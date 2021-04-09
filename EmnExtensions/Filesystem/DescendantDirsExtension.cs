using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace EmnExtensions.Filesystem
{
    public static class DescendantDirsExtension
    {
        public static IEnumerable<DirectoryInfo> TryGetDirectories(this DirectoryInfo dir) {
            return F.Swallow(() => dir.GetDirectories(), () => new DirectoryInfo[] { });
        }
        public static IEnumerable<FileInfo> TryGetFiles(this DirectoryInfo dir) {
            return F.Swallow(() => dir.GetFiles(), () => new FileInfo[] { });
        }
        public static IEnumerable<DirectoryInfo> DescendantDirs(this DirectoryInfo dir) {
            return Enumerable.Repeat(dir, 1).Concat(
                from kid in dir.TryGetDirectories()
                where (kid.Attributes & FileAttributes.ReparsePoint) == 0
                from desc in kid.DescendantDirs()
                select desc);
            //dir.GetDirectories("*",SearchOption.AllDirectories));//maybe this is symlink safe?//except that I get access denied errors :-(
        }
        public static IEnumerable<FileInfo> DescendantFiles(this DirectoryInfo dir) {
            return dir.DescendantDirs().SelectMany(subdir => subdir.TryGetFiles());
        }

        public static IEnumerable<DirectoryInfo> ParentDirs(this DirectoryInfo dir) {
            while (dir != null) {
                yield return dir;
                dir = dir.Parent;
            }
        }
    }
}
