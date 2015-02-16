#pragma once
#include "Vector2.h"
#include "ISegment.h"

namespace Engine
{
    // Implements a line segment.
    class LineSegment : public ISegment
    {
    public:
        LineSegment(Vector2<double> StartPoint, Vector2<double> EndPoint);

        // Gets the segment's position at the given offset (where offset is a value between 0.0 and 1.0).
        Vector2<double> GetPosition(double Offset) const override;

        // Gets the line segment's start position.
        Vector2<double> getStartPoint() const;

        // Gets the line segment's end position.
        Vector2<double> getEndPoint() const;

        // Gets the segment's length.
        double getLength() const override;

    private:
        Vector2<double> StartPoint_value;

        Vector2<double> EndPoint_value;

        // Sets the line segment's start position.
        void setStartPoint(Vector2<double> value);

        // Sets the line segment's end position.
        void setEndPoint(Vector2<double> value);

    };
}