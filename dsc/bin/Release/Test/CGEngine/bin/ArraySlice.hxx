#include <memory>
#include "ArraySlice.h"

using namespace stdx;

template<typename T>
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
}