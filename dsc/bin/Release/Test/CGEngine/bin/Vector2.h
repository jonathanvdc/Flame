#pragma once

namespace Engine
{
    template<typename T>
    struct Vector2
    {
        T X;

        T Y;

        Vector2(T X, T Y);

        Vector2();

    };
}

#include "Vector2.hxx"