using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using EamonExtensionsLinq.DebugTools;
namespace EamonExtensionsLinq.Filesystem {
    public static class DescendantDirsExtension {
        public static IEnumerable<DirectoryInfo> TryGetDirectories(this DirectoryInfo dir) {
            return FuncUtil.Swallow(()=>dir.GetDirectories(), ()=>new DirectoryInfo[] { });
        }
        public static IEnumerable<FileInfo> TryGetFiles(this DirectoryInfo dir) {
            return FuncUtil.Swallow(()=>dir.GetFiles(), ()=>new FileInfo[] { });
        }
        public static IEnumerable<DirectoryInfo> DescendantDirs(this DirectoryInfo dir) {
            return Enumerable.Repeat(dir, 1).Concat(
					from kid in dir.TryGetDirectories() from desc in kid.DescendantDirs() select desc);
					//dir.GetDirectories("*",SearchOption.AllDirectories));//maybe this is symlink safe?//except that I get access denied errors :-(
        }
        public static IEnumerable<FileInfo> DescendantFiles(this DirectoryInfo dir) {
			  return dir.DescendantDirs().SelectMany(subdir => subdir.TryGetFiles());
        }

    }
}
