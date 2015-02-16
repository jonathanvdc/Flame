#pragma once
#include <memory>

namespace stdx
{
    template<typename T>
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
    };
}

#include "ArraySlice.hxx"