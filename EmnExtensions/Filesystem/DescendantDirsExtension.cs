using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using EamonExtensionsLinq.DebugTools;
namespace EamonExtensionsLinq.Filesystem {
    public static class DescendantDirsExtension {
        private static T swallow<T>(Func<T> f, Func<T> err) { try { return f(); } catch (Exception) { return err(); } }
        public static IEnumerable<DirectoryInfo> TryGetDirectories(this DirectoryInfo dir) {
            return FuncUtil.Swallow(()=>dir.GetDirectories(), ()=>new DirectoryInfo[] { });
        }
        public static IEnumerable<FileInfo> TryGetFiles(this DirectoryInfo dir) {
            return FuncUtil.Swallow(()=>dir.GetFiles(), ()=>new FileInfo[] { });
        }
        public static IEnumerable<DirectoryInfo> DescendantDirs(this DirectoryInfo dir) {// comparing kid's parent fullname to current fullname to avoid descending symlink on linux
            return Enumerable.Repeat(dir, 1).Concat(from kid in dir.TryGetDirectories() where kid.Parent.FullName==dir.FullName from desc in kid.DescendantDirs() select desc);
        }
        public static IEnumerable<FileInfo> DescendantFiles(this DirectoryInfo dir) {
            return (from aDir in dir.DescendantDirs() from file in aDir.TryGetFiles() select file);
        }

    }
}
