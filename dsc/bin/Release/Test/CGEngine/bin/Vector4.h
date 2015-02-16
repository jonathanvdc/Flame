#pragma once
#include "Vector3.h"

namespace Engine
{
    template<typename T>
    struct Vector4
    {
        T X;

        T Y;

        T Z;

        T W;

        Vector4();

        Vector4(Vector3<T> XYZ, T W);

        Vector4(T X, T Y, T Z, T W);

    };
}

#include "Vector4.hxx"