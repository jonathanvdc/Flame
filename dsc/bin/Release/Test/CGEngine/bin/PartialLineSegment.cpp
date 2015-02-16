#include <memory>
#include "Vector2.h"
#include "ISegment.h"
#include "IPartialSegment.h"
#include "PartialLineSegment.h"

using namespace Engine;

// Closes the partial segment to form a curve segment.
std::shared_ptr<ISegment> PartialLineSegment::Close(Vector2<double> EndPoint) const override
{
    return std::shared_ptr<LineSegment>(new LineSegment(this->getStartPoint(), EndPoint));
}

PartialLineSegment::PartialLineSegment(Vector2<double> StartPoint)
{
    this->setStartPoint(StartPoint);
}

// Gets the line segment's start position.
Vector2<double> PartialLineSegment::getStartPoint() const override
{
    return this->StartPoint_value;
}
// Sets the line segment's start position.
void PartialLineSegment::setStartPoint(Vector2<double> value)
{
    this->StartPoint_value = value;
}