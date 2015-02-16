#pragma once
#include <memory>
#include "IPartialSegment.h"
#include "Vector2.h"
#include "IShape.h"
#include "ArraySlice.h"
#include "ISurface.h"

namespace Engine
{
    // Provides common functionality for a render context, which is a surface that supports the creation of primitive shapes.
    struct IRenderContext : public ISurface
    {
        virtual std::shared_ptr<IPartialSegment> CreateLineSegment(Vector2<double> Start) const = 0;

        virtual std::shared_ptr<IPartialSegment> CreateBezierSegment(Vector2<double> Start, Vector2<double> ControlPoint1, Vector2<double> ControlPoint2) const = 0;

        virtual std::shared_ptr<IShape> CreateShape(stdx::ArraySlice<std::shared_ptr<IPartialSegment>> Segments, Vector2<double> End) const = 0;

    };
}