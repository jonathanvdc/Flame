#pragma once
#include "Vector4.h"
#include "Vector2.h"

namespace Engine
{
    // Describes a brush: a color that can be applied to a surface.
    struct IBrush
    {
        // Gets the brush's color at the given position.
        virtual Vector4<double> GetColor(Vector2<double> Position) const = 0;

    };
}