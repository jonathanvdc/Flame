#include "Vector2.h"
#include "ISegment.h"
#include "LineSegment.h"

using namespace Engine;

LineSegment::LineSegment(Vector2<double> StartPoint, Vector2<double> EndPoint)
{
    this->setStartPoint(StartPoint);
    this->setEndPoint(EndPoint);
}

// Gets the segment's position at the given offset (where offset is a value between 0.0 and 1.0).
Vector2<double> LineSegment::GetPosition(double Offset) const override
{
    return Vector2<double>(this->getStartPoint().X + Offset * this->getEndPoint().X, this->getStartPoint().Y + Offset * this->getEndPoint().Y);
}

// Sets the line segment's start position.
void LineSegment::setStartPoint(Vector2<double> value)
{
    this->StartPoint_value = value;
}
// Gets the line segment's start position.
Vector2<double> LineSegment::getStartPoint() const
{
    return this->StartPoint_value;
}

// Sets the line segment's end position.
void LineSegment::setEndPoint(Vector2<double> value)
{
    this->EndPoint_value = value;
}
// Gets the line segment's end position.
Vector2<double> LineSegment::getEndPoint() const
{
    return this->EndPoint_value;
}

// Gets the segment's length.
double LineSegment::getLength() const override
{
    double dx = this->getEndPoint().X - this->getStartPoint().X, dy = this->getEndPoint().Y - this->getStartPoint().Y;
    return dx * dx + dy * dy;
}