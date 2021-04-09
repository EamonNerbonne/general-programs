using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EmnExtensions.IO
{
    [Obsolete]
    public abstract class LPathEntry
    {
        protected static readonly Assembly mscorlib = Assembly.GetAssembly(typeof(File));
        protected static T mkFn<T>(MethodInfo mi) => (T)(object)Delegate.CreateDelegate(typeof(T), mi);

        readonly string fullname;
        public string FullName => fullname;
        protected string RawPath => fullname;

        internal LPathEntry(string path, bool addTrailingSlash)
        {
            var rawpath = path;
            fullname = addTrailingSlash && !rawpath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? rawpath + Path.DirectorySeparatorChar : rawpath;
        }

        public override string ToString() => FullName;
    }

    [Obsolete]
    public sealed class LFile : LPathEntry, IEquatable<LFile>
    {
        static readonly Type lpf = mscorlib.GetType("System.IO.LongPathFile");
        static readonly Func<string, DateTimeOffset> getLastWriteTime = mkFn<Func<string, DateTimeOffset>>(lpf.GetMethod("GetLastWriteTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        static readonly Func<string, long> getLength = mkFn<Func<string, long>>(lpf.GetMethod("GetLength", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));

        public LFile(string path) : base(path, false) { }

        public bool Exists => File.Exists(FullName);

        public string Extension => Path.GetExtension(FullName);

        public DateTime LastWriteTime => getLastWriteTime(FullName).UtcDateTime.ToLocalTime();
        public DateTime LastWriteTimeUtc => getLastWriteTime(FullName).UtcDateTime;

        public long Length => getLength(FullName);
        public static LFile Construct(string path) => new LFile(path);
        public static LFile Construct(FileInfo fileinfo) => new LFile(fileinfo.FullName);
        public static LFile ConstructIfExists(string path) => File.Exists(path) ? new LFile(path) : null;

        public FileStream OpenRead() => OpenRead(FullName);
        public static FileStream OpenRead(string pathOrUrl) => File.Open(pathOrUrl, FileMode.Open, FileAccess.Read, FileShare.Read);

        public Stream Open(FileMode fileMode) => File.Open(FullName, fileMode, FileAccess.ReadWrite);

        public LFile Move(string newpath) { File.Move(FullName, newpath); return Construct(newpath); }

        public static bool Delete(string path)
        {
            if (File.Exists(path)) {
                File.Delete(path);
                return true;
            } else {
                return false;
            }
        }
        public bool Delete() => Delete(FullName);

        public static void Move(string path, string newpath) => File.Move(path, newpath);
        public LDirectory Directory
        {
            get {
                var fullname = FullName;
                var lastSlash = fullname.LastIndexOf(Path.DirectorySeparatorChar, fullname.Length - 1);
                return lastSlash == -1 ? null : LDirectory.Construct(FullName.Substring(0, lastSlash + 1));
            }
        }

        public string Name => Path.GetFileName(FullName);
        public FileInfo AsInfo() => new FileInfo(FullName);

        public bool Equals(LFile other) => !ReferenceEquals(other, null) && RawPath == other.RawPath;
        public static bool operator ==(LFile a, LFile b) => ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.Equals(b);
        public static bool operator !=(LFile a, LFile b) => !ReferenceEquals(a, b) && (ReferenceEquals(a, null) || !a.Equals(b));
        public override bool Equals(object obj) => Equals(obj as LFile);
        public override int GetHashCode() => RawPath.GetHashCode() + 42;
    }

    [Obsolete]
    public sealed class LDirectory : LPathEntry, IEquatable<LDirectory>
    {
        public LDirectory(string path) : base(path, true) { }

        public bool Exists => Directory.Exists(FullName);

        public static LDirectory Construct(string path) => new LDirectory(path);
        public static LDirectory Construct(DirectoryInfo dirInfo) => new LDirectory(dirInfo.FullName);

        public IEnumerable<LFile> GetFiles() => Directory.EnumerateFiles(FullName).Select(LFile.Construct);
        public IEnumerable<LFile> GetFiles(string searchPattern) => Directory.EnumerateFiles(FullName, searchPattern).Select(LFile.Construct);
        public IEnumerable<LFile> GetFiles(string searchPattern, SearchOption searchOption) => searchOption == SearchOption.TopDirectoryOnly ? GetFiles(searchPattern) : GetDescendantAndSelfDirectories().SelectMany(dir => dir.GetFiles(searchPattern));
        public IEnumerable<LDirectory> GetDirectories() => Directory.EnumerateDirectories(FullName).Select(LDirectory.Construct);
        public IEnumerable<LDirectory> GetDescendantDirectories() => GetDescendantAndSelfDirectories().Skip(1);
        IEnumerable<LDirectory> GetDescendantAndSelfDirectories()
        {
            var todo = new Stack<LDirectory>();
            todo.Push(this);
            while (todo.Count != 0) {
                var next = todo.Pop();
                yield return next;
                foreach (var kid in next.GetDirectories().Reverse()) {
                    todo.Push(kid);
                }
            }
        }

        public DirectoryInfo AsInfo() => new DirectoryInfo(FullName);


        public void Create()
        {
            if (!Exists) {
                Parent.Create();
            }

            Directory.CreateDirectory(FullName);
        }

        public LDirectory CreateSubdirectory(string subdirname)
        {
            var subdir = new LDirectory(FullName + Path.DirectorySeparatorChar + subdirname);
            subdir.Create();
            return subdir;
        }

        public LDirectory Parent
        {
            get {
                var fullname = FullName;
                var lastSlash = fullname.LastIndexOf(Path.DirectorySeparatorChar, fullname.Length - 2);
                return lastSlash == -1 ? null : LDirectory.Construct(FullName.Substring(0, lastSlash + 1));
            }
        }

        public string Name
        {
            get {
                var fullname = FullName;
                var lastSlash = fullname.LastIndexOf(Path.DirectorySeparatorChar, fullname.Length - 2);
                var name = fullname.Substring(lastSlash + 1, fullname.Length - lastSlash - 2);
                return name.EndsWith(":") && lastSlash == -1
                        ? name + Path.DirectorySeparatorChar
                        : name;
            }
        }

        public bool Equals(LDirectory other) => !ReferenceEquals(other, null) && RawPath == other.RawPath;
        public static bool operator ==(LDirectory a, LDirectory b) => ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.Equals(b);
        public static bool operator !=(LDirectory a, LDirectory b) => !ReferenceEquals(a, b) && (ReferenceEquals(a, null) || !a.Equals(b));
        public override bool Equals(object obj) => Equals(obj as LDirectory);
        public override int GetHashCode() => RawPath.GetHashCode() - 37;
    }
}
