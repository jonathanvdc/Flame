#pragma once
#include <vector>

template<typename T>
class Stack
{
public:
    Stack();

    void Push(T Item);

    T Pop();

    T Peek() const;

private:
    std::vector<T> data = std::vector<T>();

};

#include "Stack.hxx"