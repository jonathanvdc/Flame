using Flame.Build;
using Flame.CodeDescription;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    /// <summary>
    /// Describes the environment within a C++ templated member.
    /// </summary>
    public class TemplatedMemberCppEnvironment : ICppEnvironment
    {
        public TemplatedMemberCppEnvironment(ICppEnvironment DeclaringEnvironment, IConverter<IType, IType> TemplateConverter)
        {
            this.DeclaringEnvironment = DeclaringEnvironment;
            this.TemplateConverter = TemplateConverter;
        }
        public TemplatedMemberCppEnvironment(ICppEnvironment DeclaringEnvironment, IGenericMember TemplatedMember)
        {
            this.DeclaringEnvironment = DeclaringEnvironment;
            this.TemplateConverter = new TypeParameterConverter(TemplatedMember);
        }

        public ICppEnvironment DeclaringEnvironment { get; private set; }
        public IConverter<IType, IType> TemplateConverter { get; private set; }

        public ICompilerLog Log
        {
            get { return DeclaringEnvironment.Log; }
        }

        public DocumentationCommentBuilder DocumentationBuilder
        {
            get { return DeclaringEnvironment.DocumentationBuilder; }
        }

        public ICppTypeConverter TypeConverter
        {
            get { return new CompositeCppTypeConverter(TemplateConverter, DeclaringEnvironment.TypeConverter); }
        }

        public Func<INamespace, IConverter<IType, string>> TypeNamer
        {
            get { return DeclaringEnvironment.TypeNamer; }
        }

        public IType EnumerableType
        {
            get { return DeclaringEnvironment.EnumerableType; }
        }

        public IType EnumeratorType
        {
            get { return DeclaringEnvironment.EnumeratorType; }
        }

        public IEnumerable<IType> GetDefaultBaseTypes(
            IType Type, IEnumerable<IType> DefaultBaseTypes)
        {
            return DeclaringEnvironment.GetDefaultBaseTypes(Type, DefaultBaseTypes);
        }

        public string Name
        {
            get { return DeclaringEnvironment.Name; }
        }

        public IType RootType
        {
            get { return DeclaringEnvironment.RootType; }
        }

        public ITypeDefinitionPacker TypeDefinitionPacker
        {
            get { return DeclaringEnvironment.TypeDefinitionPacker; }
        }

        public TypeDependencyCache DependencyCache
        {
            get { return DeclaringEnvironment.DependencyCache; }
        }

        public IEnumerable<INamespace> StandardNamespaces
        {
            get { return DeclaringEnvironment.StandardNamespaces; }
        }
    }
}
