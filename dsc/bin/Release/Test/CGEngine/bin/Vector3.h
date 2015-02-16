#pragma once
#include "Vector2.h"

namespace Engine
{
    // Describes a three-dimensional vector.
    template<typename T>
    struct Vector3
    {
        T X;

        T Y;

        T Z;

        Vector3();

        Vector3(Vector2<T> XY, T Z);

        Vector3(T X, T Y, T Z);

    };
}

#include "Vector3.hxx"