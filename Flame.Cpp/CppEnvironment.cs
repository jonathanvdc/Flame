using Flame.CodeDescription;
using Flame.Compiler;
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
            : this(TypeConverter, TypeNamer, CreateDocumentationBuilder(new DoxygenFormatter()))
        {
        }
        public CppEnvironment(ICppTypeConverter TypeConverter, Func<INamespace, IConverter<IType, string>> TypeNamer, DocumentationCommentBuilder DocumentationBuilder)
        {
            this.TypeConverter = TypeConverter;
            this.TypeNamer = TypeNamer;
            this.DocumentationBuilder = DocumentationBuilder;
            this.DependencyCache = new TypeDependencyCache();
        }
        public CppEnvironment(DocumentationCommentBuilder DocumentationBuilder)
            : this()
        {
            this.DocumentationBuilder = DocumentationBuilder;
        }

        public DocumentationCommentBuilder DocumentationBuilder { get; private set; }
        public ICppTypeConverter TypeConverter { get; private set; }
        public Func<INamespace, IConverter<IType, string>> TypeNamer { get; private set; }
        public TypeDependencyCache DependencyCache { get; private set; }

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

        public static DocumentationCommentBuilder CreateDocumentationBuilder(IDocumentationFormatter Formatter)
        {
            return new DocumentationCommentBuilder(Formatter, docs => DocumentationExtensions.ToLineComments(docs, "///"));
        }

        public static CppEnvironment Create(ICompilerLog Log)
        {
            var formatter = Log.Options.GetDocumentationFormatter(new DoxygenFormatter());
            var docBuilder = CreateDocumentationBuilder(formatter);
            return new CppEnvironment(docBuilder);
        }

        public ITypeDefinitionPacker TypeDefinitionPacker
        {
            get { return DefaultTypeDefinitionPacker.Instance; }
        }
    }
}
