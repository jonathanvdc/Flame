#pragma once
#include <memory>
#include "ISegment.h"
#include "Vector2.h"

namespace Engine
{
    // Describes a partial segment: a segment that has not been closed yet.
    struct IPartialSegment
    {
        // Closes the partial segment to form a curve segment.
        virtual std::shared_ptr<ISegment> Close(Vector2<double> EndPoint) const = 0;

        // Gets the partial segment's start point.
        virtual Vector2<double> getStartPoint() const = 0;

    };
}