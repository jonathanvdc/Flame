#include "Vector4.h"
#include "Vector2.h"
#include "IBrush.h"
#include "SolidColorBrush.h"

using namespace Engine;

// Gets the brush's color at the given position.
Vector4<double> SolidColorBrush::GetColor(Vector2<double> Position) const override
{
    return this->getColor();
}

// Creates a new solid color brush.
SolidColorBrush::SolidColorBrush(Vector4<double> Color)
{
    this->setColor(Color);
}

// Gets the brush's color.
Vector4<double> SolidColorBrush::getColor() const
{
    return this->Color_value;
}
// Sets the brush's color.
void SolidColorBrush::setColor(Vector4<double> value)
{
    this->Color_value = value;
}