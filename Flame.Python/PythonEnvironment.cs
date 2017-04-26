using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonEnvironment : IEnvironment
    {
        private PythonEnvironment()
        {

        }

        static PythonEnvironment()
        {
            Instance = new PythonEnvironment();
        }

        public static PythonEnvironment Instance { get; private set; }

        public string Name
        {
            get { return "Python"; }
        }

        public IType RootType
        {
            get { return PythonObjectType.Instance; }
        }

        public IType EnumerableType
        {
            get { return PythonIterableType.Instance; }
        }

        public IType EnumeratorType
        {
            get { return PythonIteratorType.Instance; }
        }

        public IEnumerable<IType> GetDefaultBaseTypes(
            IType Type, IEnumerable<IType> DefaultBaseTypes)
        {
            if (Type.GetIsInterface())
                return Enumerable.Empty<IType>();

            foreach (var baseTy in DefaultBaseTypes)
            {
                if (!baseTy.GetIsInterface())
                    return Enumerable.Empty<IType>();
            }
            return new IType[] { RootType };
        }

        /// <inheritdoc/>
        public IType GetEquivalentType(IType Type)
        {
            return Type;
        }
    }
}
