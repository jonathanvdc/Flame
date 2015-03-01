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
            this.ElementType = new DescribedGenericParameter("T", this);
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


        private IMethod[] ctorCache;
        public override IMethod[] GetConstructors()
        {
            if (ctorCache == null)
            {
                List<IMethod> ctors = new List<IMethod>();

                var lenCtor = new DescribedMethod("ArraySlice", this, PrimitiveTypes.Void, false);
                lenCtor.IsConstructor = true;
                lenCtor.AddParameter(new DescribedParameter("Length", PrimitiveTypes.Int32));
                lenCtor.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                ctors.Add(lenCtor);

                var ptrCtor = new DescribedMethod("ArraySlice", this, PrimitiveTypes.Void, false);
                ptrCtor.IsConstructor = true;
                ptrCtor.AddParameter(new DescribedParameter("Pointer", ElementType.MakePointerType(PointerKind.TransientPointer)));
                ptrCtor.AddParameter(new DescribedParameter("Length", PrimitiveTypes.Int32));
                ptrCtor.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                ctors.Add(ptrCtor);

                ctorCache = ctors.ToArray();
            }
            return ctorCache;
        }

        public override IField[] GetFields()
        {
            return new IField[0];
        }

        private IMethod[] methodCache;
        public override IMethod[] GetMethods()
        {
            if (methodCache == null)
            {
                List<IMethod> methods = new List<IMethod>();

                var sliceCountMethod = new DescribedMethod("Slice", this, this.MakeGenericType(new IType[] { ElementType }), false);
                sliceCountMethod.AddParameter(new DescribedParameter("Offset", PrimitiveTypes.Int32));
                sliceCountMethod.AddParameter(new DescribedParameter("Count", PrimitiveTypes.Int32));
                sliceCountMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
                sliceCountMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                methods.Add(sliceCountMethod);

                var sliceOffsetMethod = new DescribedMethod("Slice", this, this.MakeGenericType(new IType[] { ElementType }), false);
                sliceOffsetMethod.AddParameter(new DescribedParameter("Offset", PrimitiveTypes.Int32));
                sliceOffsetMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
                sliceOffsetMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                methods.Add(sliceOffsetMethod);

                methodCache = methods.ToArray();
            }
            return methodCache;
        }

        private IProperty[] propertyCache;
        public override IProperty[] GetProperties()
        {
            if (propertyCache == null)
            {
                List<IProperty> props = new List<IProperty>();

                var lenProp = new DescribedProperty("Length", this, PrimitiveTypes.Int32, false);
                var getLenAccessor = new DescribedAccessor("GetLength", AccessorType.GetAccessor, lenProp, PrimitiveTypes.Int32);
                getLenAccessor.AddAttribute(new AccessAttribute(AccessModifier.Public));
                getLenAccessor.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                lenProp.AddAccessor(getLenAccessor);
                props.Add(lenProp);

                propertyCache = props.ToArray();
            }
            return propertyCache;
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
	ArraySlice();
    ArraySlice(int Length);
	ArraySlice(std::shared_ptr<std::vector<T>> Array, int Length);
    ArraySlice(std::initializer_list<T> Values);
    ArraySlice(T* Array, int Length);
	ArraySlice(const ArraySlice<T>& Other);

    T& operator[](int Index);
    const T& operator[](int Index) const;
	ArraySlice<T>& operator=(const ArraySlice<T>& Other);

    ArraySlice<T> Slice(int Start, int Length) const;
    ArraySlice<T> Slice(int Start) const;

    int GetLength() const;

private:
	ArraySlice(std::shared_ptr<std::vector<T>> Array, int Offset, int Length);

    std::shared_ptr<std::vector<T>> ptr;
    int offset, length;
};";
        private const string HeaderImplementationCode =
@"template<typename T>
ArraySlice<T>::ArraySlice()
	: ptr(std::make_shared<std::vector<T>>()), length(0), offset(0)
{
}

template<typename T>
ArraySlice<T>::ArraySlice(int Length)
	: ptr(std::make_shared<std::vector<T>>(Length)), length(Length), offset(0)
{
}

template<typename T>
ArraySlice<T>::ArraySlice(std::shared_ptr<std::vector<T>> Array, int Length)
    : ptr(Array), length(Length), offset(0)
{
}

template<typename T>
ArraySlice<T>::ArraySlice(std::initializer_list<T> Values)
	: ptr(std::make_shared<std::vector<T>>(Values)), offset(0)
{
	this->length = this->ptr->size();
}

template<typename T>
ArraySlice<T>::ArraySlice(T* Array, int Length)
	: ptr(std::make_shared<std::vector<T>>(Length)), length(Length), offset(0)
{
    for (int i = 0; i < Length; i++)
    {
        (*this)[i] = Array[i];
    }
}

template<typename T>
ArraySlice<T>::ArraySlice(std::shared_ptr<std::vector<T>> Array, int Offset, int Length)
    : ptr(Array), length(Length), offset(Offset)
{
}

template<typename T>
ArraySlice<T>::ArraySlice(const ArraySlice<T>& Other)
	: ptr(Other.ptr), length(Other.length), offset(Other.offset)
{

}

template<typename T>
T& ArraySlice<T>::operator[](int Index)
{
    return this->ptr->at(this->offset + Index);
}

template<typename T>
const T& ArraySlice<T>::operator[](int Index) const
{
	return this->ptr->at(this->offset + Index);
}

template<typename T>
ArraySlice<T>& ArraySlice<T>::operator=(const ArraySlice<T>& Other)
{
	this->ptr = Other.ptr;
	this->length = Other.length;
	this->offset = Other.offset;
    return *this;
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
            get { return new IHeaderDependency[] { StandardDependency.Memory, StandardDependency.InitializerList, StandardDependency.Vector }; }
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
