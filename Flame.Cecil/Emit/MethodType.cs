using Flame.Build;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    /*public class MethodType : IType, IMethod
    {
        protected MethodType(IMethod Method)
        {
            this.Method = Method;
        }

        public IMethod Method { get; private set; }

        public static IType Create(IMethod Method)
        {
            var mType = Method as IType;
            if (mType != null)
            {
                return mType;
            }
            else
            {
                return new MethodType(Method);
            }
        }

        #region IMethod Implementation

        public IMethod[] GetBaseMethods()
        {
            return Method.GetBaseMethods();
        }

        public IMethod GetGenericDeclaration()
        {
            return new MethodType(Method.GetGenericDeclaration());
        }

        public IParameter[] GetParameters()
        {
            return Method.GetParameters();
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return Method.Invoke(Caller, Arguments);
        }

        public bool IsConstructor
        {
            get { return Method.IsConstructor; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return new MethodType(Method.MakeGenericMethod(TypeArguments));
        }

        public IType ReturnType
        {
            get { return Method.ReturnType; }
        }

        public IType DeclaringType
        {
            get { return Method.DeclaringType; }
        }

        public bool IsStatic
        {
            get { return Method.IsStatic; }
        }

        public string FullName
        {
            get { return Method.FullName; }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Method.GetAttributes();
        }

        public string Name
        {
            get { return Method.Name; }
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return Method.GetGenericArguments();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Method.GetGenericParameters();
        }

        #endregion

        #region IType Implementation

        public IContainerType AsContainerType()
        {
            return null;
        }

        public INamespace DeclaringNamespace
        {
            get { return DeclaringType.DeclaringNamespace; }
        }

        public IType[] GetBaseTypes()
        {
            return new IType[0];
        }

        public IMethod[] GetConstructors()
        {
            return new IMethod[0];
        }

        public IBoundObject GetDefaultValue()
        {
            return new NullExpression();
        }

        public IField[] GetFields()
        {
            return new IField[0];
        }

        IType IType.GetGenericDeclaration()
        {
            return new MethodType(Method.GetGenericDeclaration());
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
            return new MethodType(Method.MakeGenericMethod(TypeArguments));
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is MethodType)
            {
                return Method.Equals(((MethodType)obj).Method);
            }
            else
            {
                return Method.Equals(obj);
            }
        }
        public override int GetHashCode()
        {
            return Method.GetHashCode();
        }
        public override string ToString()
        {
            return Method.ToString();
        }
    }*/
}
