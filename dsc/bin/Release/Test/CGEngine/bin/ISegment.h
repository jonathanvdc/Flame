#pragma once
#include "Vector2.h"

namespace Engine
{
    // Provides common functionality for curve segments.
    struct ISegment
    {
        // Gets the segment's position at the given offset (where offset is a value between 0.0 and 1.0).
        virtual Vector2<double> GetPosition(double Offset) const = 0;

        // Gets the segment's length.
        virtual double getLength() const = 0;

    };
}