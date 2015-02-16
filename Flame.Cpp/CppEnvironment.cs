using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppEnvironment : ICppEnvironment
    {
        public CppEnvironment()
        {
            this.TypeConverter = new DefaultCppTypeConverter();
            this.TypeNamer = (ns) => new CppSize32Namer(ns);
        }
        public CppEnvironment(ICppTypeConverter TypeConverter, Func<INamespace, IConverter<IType, string>> TypeNamer)
        {
            this.TypeConverter = TypeConverter;
            this.TypeNamer = TypeNamer;
        }

        public ICppTypeConverter TypeConverter { get; private set; }
        public Func<INamespace, IConverter<IType, string>> TypeNamer { get; private set; }

        public IType EnumerableType
        {
            get { return null; }
        }

        public IType EnumeratorType
        {
            get { return null; }
        }

        public string Name
        {
            get { return "C++"; }
        }

        public IType RootType
        {
            get { return null; }
        }
    }
}
