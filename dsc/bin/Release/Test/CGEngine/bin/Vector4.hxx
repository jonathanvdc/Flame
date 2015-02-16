#include "Vector3.h"
#include "Vector4.h"

using namespace Engine;

template<typename T>
Vector4<T>::Vector4()
{ }

template<typename T>
Vector4<T>::Vector4(Vector3<T> XYZ, T W)
{
    this->W = W;
    this->X = (T)XYZ.X;
    this->Y = (T)XYZ.Y;
    this->Z = (T)XYZ.Z;
}

template<typename T>
Vector4<T>::Vector4(T X, T Y, T Z, T W)
{
    this->X = X;
    this->Y = Y;
    this->Z = Z;
    this->W = W;
}