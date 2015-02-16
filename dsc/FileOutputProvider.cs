using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public class FileOutputProvider : IOutputProvider, IDisposable
    {
        public FileOutputProvider(string DirectoryName, string FileName)
        {
            this.FileName = FileName;
            this.DirectoryName = DirectoryName;
            this.files = new HashSet<PathOutputFile>();
        }

        public string FileName { get; private set; }
        public string DirectoryName { get; private set; }
        private HashSet<PathOutputFile> files;

        private string GetPath(string Name, string Extension)
        {
            StringBuilder sb = new StringBuilder(Name);
            sb.Replace('.', System.IO.Path.DirectorySeparatorChar);
            foreach (var item in Path.GetInvalidPathChars())
            {
                sb.Replace(item.ToString(), "");
            }
            sb.Append('.');
            sb.Append(Extension);
            return System.IO.Path.Combine(DirectoryName, sb.ToString());
        }

        public IOutputFile Create()
        {
            Directory.CreateDirectory(DirectoryName);
            var file = new PathOutputFile(FileName);
            files.Add(file);
            return file;
        }

        public IOutputFile Create(string Name, string Extension)
        {
            string fileName = GetPath(Name, Extension);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));
            var file = new PathOutputFile(fileName);
            files.Add(file);
            return file;
        }

        public bool PreferSingleOutput
        {
            get { return false; }
        }

        public void Dispose()
        {
            foreach (var item in files)
            {
                item.Dispose();
            }
        }

        public bool Exists(string Name, string Extension)
        {
            return files.Contains(new PathOutputFile(GetPath(Name, Extension)));
        }
    }

    public class PathOutputFile : IOutputFile, IDisposable, IEquatable<PathOutputFile>
    {
        public PathOutputFile(string Path)
        {
            this.Path = Path;
        }

        public string Path { get; private set; }
        private FileStream stream;

        public Stream OpenOutput()
        {
            stream = new FileStream(Path, FileMode.Create);
            return stream;
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
            }
        }

        public bool Equals(PathOutputFile other)
        {
            return Path == other.Path;
        }
        public override bool Equals(object obj)
        {
            if (obj is PathOutputFile)
            {
                return Equals((PathOutputFile)obj);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }
}
