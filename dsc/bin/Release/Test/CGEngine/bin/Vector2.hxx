#include "Vector2.h"

using namespace Engine;

template<typename T>
Vector2<T>::Vector2(T X, T Y)
{
    this->X = X;
    this->Y = Y;
}

template<typename T>
Vector2<T>::Vector2()
{ }