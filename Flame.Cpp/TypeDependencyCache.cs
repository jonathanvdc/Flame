using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class TypeDependencyCache
    {
        public TypeDependencyCache(bool CppTypesOnly)
        {
            this.directDepends = new Dictionary<IType, IEnumerable<IType>>();
            this.allDepends = new Dictionary<IType, IEnumerable<IType>>();
            this.CppTypesOnly = CppTypesOnly;
        }
        public TypeDependencyCache()
            : this(true)
        {
        }

        public bool CppTypesOnly { get; private set; }
        private Dictionary<IType, IEnumerable<IType>> directDepends;
        private Dictionary<IType, IEnumerable<IType>> allDepends;

        public IEnumerable<IType> GetDirectTypeDependencies(IType Type)
        {
            if (directDepends.ContainsKey(Type))
            {
                return directDepends[Type];
            }
            else
            {
                IEnumerable<IType> depends;
                if (Type is CppType || !CppTypesOnly)
                {
                    depends = Type.GetDirectTypeDependencies();
                }
                else
	            {
                    depends = Enumerable.Empty<IType>();
	            }
                directDepends[Type] = depends;
                return depends;
            }
        }

        public IEnumerable<IType> GetAllTypeDependencies(IType Type)
        {
            if (allDepends.ContainsKey(Type))
            {
                return allDepends[Type];
            }
            else
            {
                var directDepends = GetDirectTypeDependencies(Type).ToArray();
                IEnumerable<IType> allDeps = directDepends;
                while (directDepends.Any())
                {
                    directDepends = directDepends.SelectMany(GetDirectTypeDependencies).Except(allDeps).Distinct().ToArray();
                    allDeps = allDeps.Concat(directDepends);
                }
                allDepends[Type] = allDeps;
                return allDeps;
            }
        }

        public bool DependsOn(IType Type, IType Other)
        {
            return !Type.Equals(Other) && GetAllTypeDependencies(Type).Contains(Other);
        }

        public IEnumerable<IType> GetCyclicDependencies(IType Type)
        {
            return GetAllTypeDependencies(Type).Where(item => DependsOn(item, Type));
        }

        public IEnumerable<IType> GetCyclicDependencies(IEnumerable<IType> Types)
        {
            return Types.SelectMany(GetCyclicDependencies);
        }
    }
}
