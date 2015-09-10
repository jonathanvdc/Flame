using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public class FileOutputProvider : IOutputProvider, IDisposable
    {
        public FileOutputProvider(PathIdentifier DirectoryName, PathIdentifier FileName, bool ForceWrite)
        {
            this.FileName = FileName;
            this.DirectoryName = DirectoryName;
            this.ForceWrite = ForceWrite;
            this.files = new HashSet<PathOutputFile>();
        }

        public PathIdentifier FileName { get; private set; }
        public PathIdentifier DirectoryName { get; private set; }
        public bool ForceWrite { get; private set; }
        public bool AnyFilesOverwritten { get { return files.Any(item => item.FileOverwritten); } }

        private HashSet<PathOutputFile> files;

        private PathIdentifier GetPath(string Name, string Extension)
        {
            StringBuilder sb = new StringBuilder(Name);
            sb.Replace('.', System.IO.Path.DirectorySeparatorChar);
            foreach (var item in Path.GetInvalidPathChars())
            {
                sb.Replace(item.ToString(), "");
            }
            sb.Append('.');
            sb.Append(Extension);
            return DirectoryName.Combine(sb.ToString());
        }

        public IOutputFile Create()
        {
            Directory.CreateDirectory(DirectoryName.Path);
            var file = new PathOutputFile(FileName, ForceWrite);
            files.Add(file);
            return file;
        }

        public IOutputFile Create(string Name, string Extension)
        {
            var fileName = GetPath(Name, Extension);
            Directory.CreateDirectory(fileName.Parent.Path);
            var file = new PathOutputFile(fileName, ForceWrite);
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
            return files.Contains(new PathOutputFile(GetPath(Name, Extension), ForceWrite));
        }
    }

    public class PathOutputFile : IOutputFile, IDisposable, IEquatable<PathOutputFile>
    {
        public PathOutputFile(PathIdentifier Path, bool ForceWrite)
        {
            this.Path = Path;
            this.ForceWrite = ForceWrite;
            this.FileOverwritten = false;
        }

        public PathIdentifier Path { get; private set; }
        public bool ForceWrite { get; private set; }
        public bool FileOverwritten { get; private set; }

        private Stream stream;

        public Stream OpenOutput()
        {
            string fileName = Path.Path;
            stream = ForceWrite || !File.Exists(fileName) ? new FileStream(fileName, FileMode.Create) : (Stream)new PreservingFileStream(fileName);
            return stream;
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();

                if (stream is PreservingFileStream)
                {
                    FileOverwritten = ((PreservingFileStream)stream).WriteToFile();
                }
                else
                {
                    FileOverwritten = true;
                }
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
