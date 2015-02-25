using Flame.CodeDescription;
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
            : this(new DefaultCppTypeConverter(), (ns) => new CppSize32Namer(ns))
        {
        }
        public CppEnvironment(ICppTypeConverter TypeConverter, Func<INamespace, IConverter<IType, string>> TypeNamer)
            : this(TypeConverter, TypeNamer, new DocumentationCommentBuilder(new DoxygenFormatter(), (docs) => DocumentationExtensions.ToLineComments(docs, "///")))
        {
        }
        public CppEnvironment(ICppTypeConverter TypeConverter, Func<INamespace, IConverter<IType, string>> TypeNamer, IDocumentationCommentBuilder DocumentationBuilder)
        {
            this.TypeConverter = TypeConverter;
            this.TypeNamer = TypeNamer;
            this.DocumentationBuilder = DocumentationBuilder;
        }

        public IDocumentationCommentBuilder DocumentationBuilder { get; private set; }
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
