using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
namespace std
{
    public struct vector<T>
    {
        public const this();
        public const this(int Length);
        public const this(ref vector<T> Other);
        
        public const int size();
        
        public void push_back(T Value);
        public void pop_back(T Value);  
        
        public T this[int Index]
        {
            const get; set;
        }
        
        public const T front() { return this[0]; }
        public const T back() { return this[size() - 1]; }
 
        public const T at(int Index) { return this[Index]; }
   
        public void swap(ref vector<T> Other);
 
        public struct iterator
        {
            
        }
 
        public const iterator begin();
        public const iterator end();
    }
}
*/

namespace Flame.Cpp.Plugs
{
    /// <summary>
    /// Describes a subset of the std::vector&lt;T&gt; type, mainly for array behavior.
    /// </summary>
    public class StdVectorType : StdTemplatedTypeBase, IArrayType
    {
        private StdVectorType()
        {
            this.ElementType = new DescribedGenericParameter("T", this);
        }

        public IGenericParameter ElementType { get; private set; }
        public override IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[] { ElementType };
        }

        public override string Name
        {
            get { return "vector<>"; }
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { new AccessAttribute(AccessModifier.Public) };
        }

        public override IMethod[] GetConstructors()
        {
            throw new NotImplementedException();
        }

        public override IField[] GetFields()
        {
            throw new NotImplementedException();
        }

        public override IMethod[] GetMethods()
        {
            throw new NotImplementedException();
        }

        public override IProperty[] GetProperties()
        {
            throw new NotImplementedException();
        }

        #region Static

        private static StdVectorType inst = new StdVectorType();
        public static StdVectorType Instance
        {
            get
            {
                return inst;
            }
        }

        #endregion

        #region Array Functionality

        public int ArrayRank
        {
            get { return 1; }
        }

        public IArrayType AsArrayType()
        {
            return this;
        }

        public IPointerType AsPointerType()
        {
            return null;
        }

        public IVectorType AsVectorType()
        {
            return null;
        }

        public ContainerTypeKind ContainerKind
        {
            get { return ContainerTypeKind.Array; }
        }

        public IType GetElementType()
        {
            return ElementType;
        }

        #endregion
    }
}
