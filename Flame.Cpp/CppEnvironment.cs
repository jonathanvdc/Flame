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
            : this(new EmptyCompilerLog(EmptyCompilerOptions.Instance))
        {

        }
        public CppEnvironment(ICompilerLog Log)
            : this(Log, null, (ns) => new CppSize32Namer(ns))
        {
        }
        public CppEnvironment(ICompilerLog Log, ICppTypeConverter TypeConverter, Func<INamespace, IConverter<IType, string>> TypeNamer)
            : this(Log, TypeConverter, TypeNamer, CreateDocumentationBuilder(new DoxygenFormatter()))
        {
        }
        public CppEnvironment(ICompilerLog Log, ICppTypeConverter TypeConverter, Func<INamespace, IConverter<IType, string>> TypeNamer, DocumentationCommentBuilder DocumentationBuilder)
        {
            this.Log = Log;
            this.TypeConverter = TypeConverter ?? new DefaultCppTypeConverter(this);
            this.TypeNamer = TypeNamer;
            this.DocumentationBuilder = DocumentationBuilder;
            this.StandardNamespaces = new INamespace[] { new Plugs.StdxNamespace(this) };
            this.DependencyCache = new TypeDependencyCache();
        }
        public CppEnvironment(ICompilerLog Log, DocumentationCommentBuilder DocumentationBuilder)
            : this(Log)
        {
            this.DocumentationBuilder = DocumentationBuilder;
        }

        public ICompilerLog Log { get; private set; }
        public DocumentationCommentBuilder DocumentationBuilder { get; private set; }
        public ICppTypeConverter TypeConverter { get; private set; }
        public Func<INamespace, IConverter<IType, string>> TypeNamer { get; private set; }
        public TypeDependencyCache DependencyCache { get; private set; }
        public IEnumerable<INamespace> StandardNamespaces { get; private set; }

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
            return new CppEnvironment(Log, docBuilder);
        }

        public ITypeDefinitionPacker TypeDefinitionPacker
        {
            get { return DefaultTypeDefinitionPacker.Instance; }
        }

        public IEnumerable<IType> GetDefaultBaseTypes(
            IType Type, IEnumerable<IType> DefaultBaseTypes)
        {
            return Enumerable.Empty<IType>();
        }

        /// <inheritdoc/>
        public IType GetEquivalentType(IType Type)
        {
            return Type;
        }
    }
}
