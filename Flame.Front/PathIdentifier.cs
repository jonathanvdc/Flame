using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public struct PathIdentifier : IEquatable<PathIdentifier>
    {
        public PathIdentifier(string Path)
        {
            this = default(PathIdentifier);
            this.Path = Path;
        }
        public PathIdentifier(string BasePath, string RelativePath)
        {
            this = new PathIdentifier(BasePath).Combine(RelativePath);
        }
        public PathIdentifier(PathIdentifier BasePath, string RelativePath)
        {
            this = BasePath.Combine(RelativePath);
        }
        public PathIdentifier(PathIdentifier BasePath, PathIdentifier RelativePath)
        {
            this = BasePath.Combine(RelativePath);
        }

        public string Path { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Path);
            }
        }

        public string Name
        {
            get
            {
                return System.IO.Path.GetFileName(Path);
            }
        }

        public string NameWithoutExtension
        {
            get
            {
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            }
        }

        public string Extension
        {
            get
            {
                return System.IO.Path.GetExtension(Path).TrimStart('.');
            }
        }

        public PathIdentifier AbsolutePath
        {
            get
            {
                return new PathIdentifier(System.IO.Path.GetFullPath(Path));
            }
        }

        public PathIdentifier GetRelativePath(PathIdentifier BasePath)
        {
            var basePath = new Uri(BasePath.Path);
            var fullPath = new Uri(Path);

            var relativeUri = fullPath.MakeRelativeUri(basePath);

            return new PathIdentifier(relativeUri.LocalPath);
        }

        public PathIdentifier GetAbsolutePath(string RelativePath)
        {
            var relUri = new Uri(RelativePath, UriKind.RelativeOrAbsolute);
            var baseUri = new Uri(Path, UriKind.RelativeOrAbsolute);
            var absUri = new Uri(baseUri, relUri);
            return new PathIdentifier(absUri.LocalPath);
        }

        public PathIdentifier GetAbsolutePath(PathIdentifier RelativePath)
        {
            return GetAbsolutePath(RelativePath.Path);
        }

        public PathIdentifier Parent
        {
            get
            {
                return new PathIdentifier(System.IO.Path.GetDirectoryName(Path));
            }
        }

        public PathIdentifier ChangeExtension(string Extension)
        {
            return new PathIdentifier(System.IO.Path.ChangeExtension(Path, Extension));
        }

        public PathIdentifier AppendExtension(string Extension)
        {
            return new PathIdentifier(Path + '.' + Extension);
        }

        public PathIdentifier Combine(PathIdentifier Other)
        {
            return new PathIdentifier(System.IO.Path.Combine(Path, Other.Path));
        }

        public PathIdentifier Combine(string Other)
        {
            return new PathIdentifier(System.IO.Path.Combine(Path, Other));
        }

        public override string ToString()
        {
            return Path.ToString();
        }

        public static bool operator ==(PathIdentifier Left, PathIdentifier Right)
        {
            return Left.Path == Right.Path;
        }
        public static bool operator !=(PathIdentifier Left, PathIdentifier Right)
        {
            return Left.Path != Right.Path;
        }

        public override bool Equals(object obj)
        {
            if (obj is PathIdentifier)
            {
                return Equals((PathIdentifier)obj);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(PathIdentifier other)
        {
            return Path.Equals(other.Path);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        public static PathIdentifier Parse(string Value)
        {
            return new PathIdentifier(Value);
        }
    }

    public class AbsolutePathComparer : IEqualityComparer<PathIdentifier>
    {
        private AbsolutePathComparer()
        { }

        public static readonly AbsolutePathComparer Instance = new AbsolutePathComparer();

        public bool Equals(PathIdentifier x, PathIdentifier y)
        {
            return x.AbsolutePath == y.AbsolutePath;
        }

        public int GetHashCode(PathIdentifier obj)
        {
            return obj.AbsolutePath.GetHashCode();
        }
    }
}
