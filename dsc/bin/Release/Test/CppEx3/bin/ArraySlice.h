#pragma once
#include <memory>

class ArraySlice
{

private:
    std::shared_ptr<int> arr = nullptr;

    int len = 0;

public:
    ArraySlice(std::shared_ptr<int> arr, int len);

    int getLength() const;

    int getItem(const int Index) const;
    void setItem(int Index, int value);

};