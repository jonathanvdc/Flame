#pragma once
#include <memory>
#include "ArraySlice.h"


ArraySlice::ArraySlice(std::shared_ptr<int> arr, int len)
{
    this->arr = arr;
    this->len = len;
}

int ArraySlice::getLength() const
{
    return this->len;
}

int ArraySlice::getItem(const int Index) const
{
    return *(this->arr.get() + Index);
}
void ArraySlice::setItem(int Index, int value)
{
    *(this->arr.get() + Index) = value
}