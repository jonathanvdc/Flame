#pragma once
#include <memory>
#include "Vector2.h"
#include "ISegment.h"
#include "IPartialSegment.h"

namespace Engine
{
    // Describes a partial line segment.
    class PartialLineSegment : public IPartialSegment
    {
    public:
        // Closes the partial segment to form a curve segment.
        std::shared_ptr<ISegment> Close(Vector2<double> EndPoint) const override;

        PartialLineSegment(Vector2<double> StartPoint);

        // Gets the line segment's start position.
        Vector2<double> getStartPoint() const override;

    private:
        Vector2<double> StartPoint_value;

        // Sets the line segment's start position.
        void setStartPoint(Vector2<double> value);

    };
}