using Flame.Build;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppTemplateParameter : IGenericParameter, ICppMember
    {
        public CppTemplateParameter(IGenericMember DeclaringMember, IGenericParameter Template, ICppEnvironment Environment)
        {
            this.DeclaringMember = DeclaringMember;
            this.Template = Template;
            this.Environment = Environment;
        }

        public IGenericMember DeclaringMember { get; private set; }
        public IGenericParameter Template { get; private set; }
        public ICppEnvironment Environment { get; private set; }

        public IGenericConstraint Constraint
        {
            get { return Template.Constraint; }
        }
        
        public IContainerType AsContainerType()
        {
            return null;
        }

        public INamespace DeclaringNamespace
        {
            get { return null; }
        }

        public IType[] GetBaseTypes()
        {
            return Template.GetBaseTypes();
        }

        public IBoundObject GetDefaultValue()
        {
            return null;
        }

        public IMethod[] GetConstructors()
        {
            return new IMethod[0];
        }

        public IField[] GetFields()
        {
            return new IField[0];
        }

        public IType GetGenericDeclaration()
        {
            return this;
        }

        public ITypeMember[] GetMembers()
        {
            return new ITypeMember[0];
        }

        public IMethod[] GetMethods()
        {
            return new IMethod[0];
        }

        public IProperty[] GetProperties()
        {
            return new IProperty[0];
        }

        public bool IsContainerType
        {
            get { return false; }
        }

        public IArrayType MakeArrayType(int Rank)
        {
            return new DescribedArrayType(this, Rank);
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return null;
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringMember.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
        }

        public string Name
        {
            get { return Template.Name; }
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[0];
        }

        public bool IsAssignable(IType Type)
        {
            return Template.IsAssignable(Type);
        }

        public IType ParameterType
        {
            get { return null; }
        }

        #region ICppMember Implementation

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return GetBaseTypes().GetDependencies(this, DeclaringMember); }
        }

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("typename ");
            cb.Append(Name);
            return cb;
        }

        public bool HasSourceCode
        {
            get { return false; }
        }

        public CodeBuilder GetSourceCode()
        {
            return new CodeBuilder();
        }

        #endregion
    }
}
