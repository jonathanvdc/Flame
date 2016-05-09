using Flame.Build;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdxArraySlice : PrimitiveBase, IDeclarationDependencyMember
    {
        public StdxArraySlice(StdxNamespace Namespace)
        {
            this.Namespace = Namespace;
            this.ElementType = new DescribedGenericParameter("T", this);
        }

        public StdxNamespace Namespace { get; private set; }
        public ICppEnvironment Environment { get { return Namespace.Environment; } }
        public IGenericParameter ElementType { get; private set; }
        public override IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[] { ElementType }; }
        }

        public override string Name
        {
            get { return "ArraySlice"; }
        }

        private static readonly AttributeMap attrMap = new AttributeMap(new IAttribute[] 
        { 
            PrimitiveAttributes.Instance.ValueTypeAttribute, 
            new AccessAttribute(AccessModifier.Public) 
        });
        public override AttributeMap Attributes
        {
            get { return attrMap; }
        }

        public override INamespace DeclaringNamespace
        {
            get { return Namespace; }
        }

        protected override IMethod[] CreateMethods()
        {
            var results = new List<IMethod>();

            var lenCtor = new DescribedMethod("ArraySlice", this, PrimitiveTypes.Void, false);
            lenCtor.IsConstructor = true;
            lenCtor.AddParameter(new DescribedParameter("Length", PrimitiveTypes.Int32));
            lenCtor.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
            results.Add(lenCtor);

            var ptrCtor = new DescribedMethod("ArraySlice", this, PrimitiveTypes.Void, false);
            ptrCtor.IsConstructor = true;
            ptrCtor.AddParameter(new DescribedParameter("Pointer", ElementType.MakePointerType(PointerKind.TransientPointer)));
            ptrCtor.AddParameter(new DescribedParameter("Length", PrimitiveTypes.Int32));
            ptrCtor.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
            results.Add(ptrCtor);

            var initListCtor = new DescribedMethod("ArraySlice", this, PrimitiveTypes.Void, false);
            initListCtor.IsConstructor = true;
            initListCtor.AddParameter(new DescribedParameter("Values", StdInitializerList.Instance.MakeGenericType(new IType[] { ElementType })));
            initListCtor.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
            results.Add(initListCtor);

            var sliceCountMethod = new DescribedMethod("Slice", this, this.MakeGenericType(new IType[] { ElementType }), false);
            sliceCountMethod.AddParameter(new DescribedParameter("Offset", PrimitiveTypes.Int32));
            sliceCountMethod.AddParameter(new DescribedParameter("Count", PrimitiveTypes.Int32));
            sliceCountMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
            sliceCountMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
            results.Add(sliceCountMethod);

            var sliceOffsetMethod = new DescribedMethod("Slice", this, this.MakeGenericType(new IType[] { ElementType }), false);
            sliceOffsetMethod.AddParameter(new DescribedParameter("Offset", PrimitiveTypes.Int32));
            sliceOffsetMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
            sliceOffsetMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
            results.Add(sliceOffsetMethod);

            return results.ToArray();
        }

        protected override IProperty[] CreateProperties()
        {
            var props = new List<IProperty>();

            var lenProp = new DescribedProperty("Length", this, PrimitiveTypes.Int32, false);
            var getLenAccessor = new DescribedAccessor("GetLength", AccessorType.GetAccessor, lenProp, PrimitiveTypes.Int32);
            getLenAccessor.AddAttribute(new AccessAttribute(AccessModifier.Public));
            getLenAccessor.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
            lenProp.AddAccessor(getLenAccessor);
            props.Add(lenProp);

            return props.ToArray();
        }

        #region Source

        #region Code

        private const string HeaderCode =
@"template<typename T>
class ArraySlice
{
public:
	typedef int size_type;
	typedef int offset_type;
	typedef int difference_type;

	ArraySlice();
	ArraySlice(size_type Length);
	ArraySlice(std::shared_ptr<std::vector<T>> Array, size_type Length);
	ArraySlice(std::initializer_list<T> Values);
	ArraySlice(const std::vector<T>& Values);
	ArraySlice(T* Array, size_type Length);
	ArraySlice(const ArraySlice<T>& Other);

	T& operator[](size_type Index);
	const T& operator[](size_type Index) const;
	ArraySlice<T>& operator=(const ArraySlice<T>& Other);
	operator std::vector<T>() const;

	ArraySlice<T> Slice(size_type Start, size_type Length) const;
	ArraySlice<T> Slice(size_type Start) const;

	size_type GetLength() const;

	class iterator
	{
	public:
		iterator(std::shared_ptr<std::vector<T>> Pointer, size_type Index) : ptr(Pointer), index(Index) { }
		T& operator*() const { return ptr->at(index); }
		T& operator[](offset_type Index) const { return ptr->at(index + Index); }
		iterator& operator++() { this->index++; return *this; }
		iterator& operator--() { this->index--; return *this; }
		iterator operator+(offset_type Offset) const { return iterator(this->ptr, index + Offset); }
		iterator operator-(offset_type Offset) const { return iterator(this->ptr, index - Offset); }
		iterator& operator+=(offset_type Offset) const { this->index += Offset; return *this; }
		iterator& operator-=(offset_type Offset) const { this->index -= Offset; return *this; }
		bool operator==(iterator& Other) const { return this->index == Other.index; }
		bool operator!=(iterator& Other) const { return this->index != Other.index; }
		difference_type operator-(iterator& Other) const { return this->index - Other.index; }
		bool operator<(iterator& Other) const { return this->index < Other.index; }
		bool operator<=(iterator& Other) const { return this->index <= Other.index; }
		bool operator>(iterator& Other) const { return this->index > Other.index; }
		bool operator>=(iterator& Other) const { return this->index >= Other.index; }

	private:
		std::shared_ptr<std::vector<T>> ptr;
		size_type index;
	};

	class const_iterator
	{
	public:
		const_iterator(std::shared_ptr<std::vector<T>> Pointer, size_type Index) : ptr(Pointer), index(Index) { }
		const T& operator*() const { return ptr->at(index); }
		const T& operator[](offset_type Index) const { return ptr->at(index + Index); }
		const_iterator& operator++() { this->index++; return *this; }
		const_iterator& operator--() { this->index--; return *this; }
		const_iterator operator+(offset_type Offset) const { return const_iterator(this->ptr, index + Offset); }
		const_iterator operator-(offset_type Offset) const { return const_iterator(this->ptr, index - Offset); }
		const_iterator& operator+=(offset_type Offset) const { this->index += Offset; return *this; }
		const_iterator& operator-=(offset_type Offset) const { this->index -= Offset; return *this; }
		bool operator==(const_iterator& Other) const { return this->index == Other.index; }
		bool operator!=(const_iterator& Other) const { return this->index != Other.index; }
		difference_type operator-(const_iterator& Other) const { return this->index - Other.index; }
		bool operator<(const_iterator& Other) const { return this->index < Other.index; }
		bool operator<=(const_iterator& Other) const { return this->index <= Other.index; }
		bool operator>(const_iterator& Other) const { return this->index > Other.index; }
		bool operator>=(const_iterator& Other) const { return this->index >= Other.index; }

	private:
		std::shared_ptr<std::vector<T>> ptr;
		size_type index;
	};

	iterator begin();
	iterator end();
	const_iterator begin() const;
	const_iterator end() const;
	const_iterator cbegin() const;
	const_iterator cend() const;

private:

	ArraySlice(std::shared_ptr<std::vector<T>> Array, size_type Offset, size_type Length);

	std::shared_ptr<std::vector<T>> ptr;
	size_type offset, length;
};";
        private const string HeaderImplementationCode =
@"template<typename T>
ArraySlice<T>::ArraySlice()
    : ptr(std::make_shared<std::vector<T>>()), length(0), offset(0)
{
}

template<typename T>
ArraySlice<T>::ArraySlice(typename ArraySlice<T>::size_type Length)
    : ptr(std::make_shared<std::vector<T>>(Length)), length(Length), offset(0)
{
}

template<typename T>
ArraySlice<T>::ArraySlice(std::shared_ptr<std::vector<T>> Array, typename ArraySlice<T>::size_type Length)
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
ArraySlice<T>::ArraySlice(T* Array, typename ArraySlice<T>::size_type Length)
    : ptr(std::make_shared<std::vector<T>>(Length)), length(Length), offset(0)
{
	for (typename ArraySlice<T>::size_type i = 0; i < Length; i++)
    {
        (*this)[i] = Array[i];
    }
}

template<typename T>
ArraySlice<T>::ArraySlice(std::shared_ptr<std::vector<T>> Array, typename ArraySlice<T>::size_type Offset, typename ArraySlice<T>::size_type Length)
    : ptr(Array), length(Length), offset(Offset)
{
}

template<typename T>
ArraySlice<T>::ArraySlice(const ArraySlice<T>& Other)
    : ptr(Other.ptr), length(Other.length), offset(Other.offset)
{
}

template<typename T>
ArraySlice<T>::ArraySlice(const std::vector<T>& Values)
	: ptr(std::make_shared<std::vector<T>>(Values)), length(Values.size()), offset(0)
{
}

template<typename T>
T& ArraySlice<T>::operator[](typename ArraySlice<T>::size_type Index)
{
    return this->ptr->at(this->offset + Index);
}

template<typename T>
const T& ArraySlice<T>::operator[](typename ArraySlice<T>::size_type Index) const
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
ArraySlice<T>::operator std::vector<T>() const
{
	if (offset == 0 && this->GetLength() == ptr->size())
	{
		return *ptr; // Fast path. Just copy the vector.
	}
	else
	{
		std::vector<T> vals(this->GetLength()); // Slow path. Offset is not zero or length does not equal size. These things happen.
		for (typename ArraySlice<T>::size_type i = 0; i < this->GetLength(); i++)
		{
			vals[i] = (*this)[i];
		}
		return vals;
	}
}

template<typename T>
ArraySlice<T> ArraySlice<T>::Slice(typename ArraySlice<T>::size_type Start, typename ArraySlice<T>::size_type Length) const
{
    return ArraySlice<T>(this->ptr, Start + this->offset, Length);
}

template<typename T>
ArraySlice<T> ArraySlice<T>::Slice(typename ArraySlice<T>::size_type Start) const
{
    return Slice(Start, this->length - Start);
}

template<typename T>
typename ArraySlice<T>::size_type ArraySlice<T>::GetLength() const
{
    return this->length;
}

template<typename T>
typename ArraySlice<T>::iterator ArraySlice<T>::begin()
{
	return ArraySlice<T>::iterator(this->ptr, this->offset);
}

template<typename T>
typename ArraySlice<T>::iterator ArraySlice<T>::end()
{
	return ArraySlice<T>::iterator(this->ptr, this->offset + this->length);
}

template<typename T>
typename ArraySlice<T>::const_iterator ArraySlice<T>::begin() const
{
	return this->cbegin();
}

template<typename T>
typename ArraySlice<T>::const_iterator ArraySlice<T>::end() const
{
	return this->cend();
}

template<typename T>
typename ArraySlice<T>::const_iterator ArraySlice<T>::cbegin() const
{
	return ArraySlice<T>::const_iterator(this->ptr, this->offset);
}

template<typename T>
typename ArraySlice<T>::const_iterator ArraySlice<T>::cend() const
{
	return ArraySlice<T>::const_iterator(this->ptr, this->offset + this->length);
}";

        #endregion

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[] { StandardDependency.Memory, StandardDependency.InitializerList, StandardDependency.Vector }; }
        }

        public IEnumerable<IHeaderDependency> DeclarationDependencies
        {
            get { return Dependencies; }
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
    }
}
