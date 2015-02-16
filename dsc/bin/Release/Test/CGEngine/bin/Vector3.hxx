#include "Vector2.h"
#include "Vector3.h"

using namespace Engine;

template<typename T>
Vector3<T>::Vector3()
{ }

template<typename T>
Vector3<T>::Vector3(Vector2<T> XY, T Z)
{
    this->Z = Z;
    this->X = (T)XY.X;
    this->Y = (T)XY.Y;
}

template<typename T>
Vector3<T>::Vector3(T X, T Y, T Z)
{
    this->X = X;
    this->Y = Y;
    this->Z = Z;
}