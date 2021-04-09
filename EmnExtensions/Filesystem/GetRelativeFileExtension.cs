using System.IO;
namespace EmnExtensions.Filesystem
{
    public static class GetRelativeFileExtension
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static FileInfo GetRelativeFile(this DirectoryInfo dir, string relativepath) => new FileInfo(Path.Combine(dir.FullName, relativepath));
    }
}
