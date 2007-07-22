using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace EamonExtensions.Filesystem {
    public static class GetRelativeFileExtension {
        public static FileInfo GetRelativeFile(this DirectoryInfo dir,string relativepath) {
            return new FileInfo(Path.Combine(dir.FullName, relativepath));
        }
    }
}
