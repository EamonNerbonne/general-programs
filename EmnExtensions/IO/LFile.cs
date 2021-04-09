using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EmnExtensions.IO {
    [Obsolete]
    public abstract class LPathEntry {
        protected static readonly Assembly mscorlib = Assembly.GetAssembly(typeof(File));
        protected static T mkFn<T>(MethodInfo mi) { return (T)(object)Delegate.CreateDelegate(typeof(T), mi); }

        readonly string fullname;
        public string FullName { get { return fullname; } }
        protected string RawPath { get { return fullname; } }

        internal LPathEntry(string path, bool addTrailingSlash) {
            var rawpath = path;
            fullname = addTrailingSlash && !rawpath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? rawpath + Path.DirectorySeparatorChar : rawpath;
        }

        public override string ToString() { return FullName; }
    }

    [Obsolete]
    public sealed class LFile : LPathEntry, IEquatable<LFile> {
        static readonly Type lpf = mscorlib.GetType("System.IO.LongPathFile");
        static readonly Func<string, DateTimeOffset> getLastWriteTime = mkFn<Func<string, DateTimeOffset>>(lpf.GetMethod("GetLastWriteTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        static readonly Func<string, long> getLength = mkFn<Func<string, long>>(lpf.GetMethod("GetLength", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));

        public LFile(string path) : base(path, false) { }

        public bool Exists { get { return File.Exists(FullName); } }

        public string Extension { get { return Path.GetExtension(FullName); } }

        public DateTime LastWriteTime { get { return getLastWriteTime(FullName).UtcDateTime.ToLocalTime(); } }
        public DateTime LastWriteTimeUtc { get { return getLastWriteTime(FullName).UtcDateTime; } }

        public long Length { get { return getLength(FullName); } }
        public static LFile Construct(string path) { return new LFile(path); }
        public static LFile Construct(FileInfo fileinfo) { return new LFile(fileinfo.FullName); }
        public static LFile ConstructIfExists(string path) { return File.Exists(path) ? new LFile(path) : null; }

        public FileStream OpenRead() { return OpenRead(FullName); }
        public static FileStream OpenRead(string pathOrUrl) { return File.Open(pathOrUrl, FileMode.Open, FileAccess.Read, FileShare.Read); }

        public Stream Open(FileMode fileMode) { return File.Open(FullName, fileMode, FileAccess.ReadWrite); }

        public LFile Move(string newpath) { File.Move(FullName, newpath); return Construct(newpath); }

        public static bool Delete(string path) {
            if (File.Exists(path)) {
                File.Delete(path);
                return true;
            } else
                return false;
        }
        public bool Delete() { return Delete(FullName); }

        public static void Move(string path, string newpath) { File.Move(path, newpath); }
        public LDirectory Directory {
            get {
                var fullname = FullName;
                int lastSlash = fullname.LastIndexOf(Path.DirectorySeparatorChar, fullname.Length - 1);
                return lastSlash == -1 ? null : LDirectory.Construct(FullName.Substring(0, lastSlash + 1));
            }
        }

        public string Name { get { return Path.GetFileName(FullName); } }
        public FileInfo AsInfo() { return new FileInfo(FullName); }

        public bool Equals(LFile other) { return !ReferenceEquals(other, null) && RawPath == other.RawPath; }
        public static bool operator ==(LFile a, LFile b) { return ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.Equals(b); }
        public static bool operator !=(LFile a, LFile b) { return !ReferenceEquals(a, b) && (ReferenceEquals(a, null) || !a.Equals(b)); }
        public override bool Equals(object obj) { return Equals(obj as LFile); }
        public override int GetHashCode() { return RawPath.GetHashCode() + 42; }
    }
    
    [Obsolete]
    public sealed class LDirectory : LPathEntry, IEquatable<LDirectory> {
        public LDirectory(string path) : base(path, true) { }

        public bool Exists { get { return Directory.Exists(FullName); } }

        public static LDirectory Construct(string path) { return new LDirectory(path); }
        public static LDirectory Construct(DirectoryInfo dirInfo) { return new LDirectory(dirInfo.FullName); }

        public IEnumerable<LFile> GetFiles() { return Directory.EnumerateFiles(FullName).Select(LFile.Construct); }
        public IEnumerable<LFile> GetFiles(string searchPattern) { return Directory.EnumerateFiles(FullName, searchPattern).Select(LFile.Construct); }
        public IEnumerable<LFile> GetFiles(string searchPattern, SearchOption searchOption) {
            return searchOption == SearchOption.TopDirectoryOnly ? GetFiles(searchPattern) : GetDescendantAndSelfDirectories().SelectMany(dir => dir.GetFiles(searchPattern));
        }
        public IEnumerable<LDirectory> GetDirectories() { return Directory.EnumerateDirectories(FullName).Select(LDirectory.Construct); }
        public IEnumerable<LDirectory> GetDescendantDirectories() { return GetDescendantAndSelfDirectories().Skip(1); }
        IEnumerable<LDirectory> GetDescendantAndSelfDirectories() {
            Stack<LDirectory> todo = new Stack<LDirectory>();
            todo.Push(this);
            while (todo.Count != 0) {
                var next = todo.Pop();
                yield return next;
                foreach (var kid in next.GetDirectories().Reverse())
                    todo.Push(kid);
            }
        }

        public DirectoryInfo AsInfo() { return new DirectoryInfo(FullName); }


        public void Create() {
            if (!Exists)
                Parent.Create();
            Directory.CreateDirectory(FullName);
        }

        public LDirectory CreateSubdirectory(string subdirname) {
            var subdir = new LDirectory(FullName + Path.DirectorySeparatorChar + subdirname);
            subdir.Create();
            return subdir;
        }

        public LDirectory Parent {
            get {
                var fullname = FullName;
                int lastSlash = fullname.LastIndexOf(Path.DirectorySeparatorChar, fullname.Length - 2);
                return lastSlash == -1 ? null : LDirectory.Construct(FullName.Substring(0, lastSlash + 1));
            }
        }

        public string Name {
            get {
                var fullname = FullName;
                int lastSlash = fullname.LastIndexOf(Path.DirectorySeparatorChar, fullname.Length - 2);
                var name = fullname.Substring(lastSlash + 1, fullname.Length - lastSlash - 2);
                return name.EndsWith(":") && lastSlash == -1
                        ? name + Path.DirectorySeparatorChar
                        : name;
            }
        }

        public bool Equals(LDirectory other) { return !ReferenceEquals(other, null) && RawPath == other.RawPath; }
        public static bool operator ==(LDirectory a, LDirectory b) { return ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.Equals(b); }
        public static bool operator !=(LDirectory a, LDirectory b) { return !ReferenceEquals(a, b) && (ReferenceEquals(a, null) || !a.Equals(b)); }
        public override bool Equals(object obj) { return Equals(obj as LDirectory); }
        public override int GetHashCode() { return RawPath.GetHashCode() - 37; }
    }
}
