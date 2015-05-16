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
    }
}
