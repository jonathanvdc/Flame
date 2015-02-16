using Flame.Build;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdxArraySlice : PrimitiveBase, ICppMember
    {
        private StdxArraySlice()
        {

        }

        public ICppEnvironment Environment { get { return new CppEnvironment(); } }
        public IGenericParameter ElementType { get; private set; }
        public override IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[] { ElementType };
        }

        public override string Name
        {
            get { return "ArraySlice"; }
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { PrimitiveAttributes.Instance.ValueTypeAttribute, new AccessAttribute(AccessModifier.Public) };
        }

        public override INamespace DeclaringNamespace
        {
            get { return StdxNamespace.Instance; }
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

        #region Make<Container>Type

        public override IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return new StdxArraySliceInstance(TypeArguments.Single());
        }

        public override IArrayType MakeArrayType(int Rank)
        {
            if (Rank == 1)
            {
                return new StdxArraySliceInstance(this);
            }
            else
            {
                return base.MakeArrayType(Rank);
            }
        }

        #endregion

        #region Source

        #region Code

        private const string HeaderCode =
@"template<typename T>
class ArraySlice
{
public:
    ArraySlice(int Length);
    ArraySlice(std::shared_ptr<T> Array, int Length);
    ArraySlice(T* Array, int Length);

    T& operator[](int Index);
    const T& operator[](int Index) const;

    ArraySlice<T> Slice(int Start, int Length) const;
    ArraySlice<T> Slice(int Start) const;

    int GetLength() const;

private:
    ArraySlice(std::shared_ptr<T> Array, int Offset, int Length);

    const std::shared_ptr<T> ptr;
    const int offset, length;
};";
        private const string HeaderImplementationCode =
@"template<typename T>
ArraySlice<T>::ArraySlice(int Length)
    : ptr(std::shared_ptr<T>(new T[Length])), length(Length), offset(0)
{
    for (int i = 0; i < Length; i++)
    {
        (*this)[i] = T();
    }
}

template<typename T>
ArraySlice<T>::ArraySlice(std::shared_ptr<T> Array, int Length)
    : ptr(Array), length(Length), offset(0)
{
    for (int i = 0; i < Length; i++)
    {
        (*this)[i] = T();
    }
}

template<typename T>
ArraySlice<T>::ArraySlice(T* Array, int Length)
    : ptr(std::shared_ptr<T>(new T[Length])), length(Length), offset(0)
{
    for (int i = 0; i < Length; i++)
    {
        (*this)[i] = Array[i];
    }
}

template<typename T>
ArraySlice<T>::ArraySlice(std::shared_ptr<T> Array, int Offset, int Length)
    : ptr(Array), length(Length), offset(Offset)
{
}

template<typename T>
T& ArraySlice<T>::operator[](int Index)
{
    return this->ptr.get()[this->offset + Index];
}

template<typename T>
const T& ArraySlice<T>::operator[](int Index) const
{
    return this->ptr.get()[this->offset + Index];
}

template<typename T>
ArraySlice<T> ArraySlice<T>::Slice(int Start, int Length) const
{
    return ArraySlice<T>(this->ptr, Start + this->offset, Length);
}

template<typename T>
ArraySlice<T> ArraySlice<T>::Slice(int Start) const
{
    return Slice(Start, this->length - Start);
}

template<typename T>
int ArraySlice<T>::GetLength() const
{
    return this->length;
}";

        #endregion

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[] { StandardDependency.Memory }; }
        }

        public CodeBuilder GetHeaderCode()
        {
            return new CodeBuilder(HeaderCode);
        }

        public bool HasSourceCode
        {
            get { return true; }
        }

        public CodeBuilder GetSourceCode()
        {
            return new CodeBuilder(HeaderImplementationCode);
        }

        #endregion

        #region Static

        private static StdxArraySlice inst = new StdxArraySlice();
        public static StdxArraySlice Instance
        {
            get
            {
                return inst;
            }
        }

        #endregion
    }

    public class StdxArraySliceInstance : DescribedGenericTypeInstance, IArrayType
    {
        public StdxArraySliceInstance(IType ElementType)
            : base(StdxArraySlice.Instance, new IType[] { ElementType })
        {

        }

        public override bool IsContainerType
        {
            get
            {
                return true;
            }
        }

        public override IContainerType AsContainerType()
        {
            return this;
        }

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
            return TypeArguments.First();
        }

        public override bool Equals(IType other)
        {
            if (other is StdxArraySliceInstance)
            {
                return GetElementType().Equals(((StdxArraySliceInstance)other).GetElementType());
            }
            else
            {
                return false;
            }
        }
    }
}
