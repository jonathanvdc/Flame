#include <vector>
#include "Stack.h"

template<typename T>
Stack<T>::Stack()
{ }

template<typename T>
void Stack<T>::Push(T Item)
{
    this->data.push_back(Item);
}

template<typename T>
T Stack<T>::Pop()
{
    T val = this->data.back();
    this->data.pop_back();
    return val;
}

template<typename T>
T Stack<T>::Peek() const
{
    return this->data.back();
}