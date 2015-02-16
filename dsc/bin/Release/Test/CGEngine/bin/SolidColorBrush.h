#pragma once
#include "Vector4.h"
#include "Vector2.h"
#include "IBrush.h"

namespace Engine
{
    // Describes a solid color brush.
    class SolidColorBrush : public IBrush
    {
    public:
        // Gets the brush's color at the given position.
        Vector4<double> GetColor(Vector2<double> Position) const override;

        // Creates a new solid color brush.
        SolidColorBrush(Vector4<double> Color);

        // Gets the brush's color.
        Vector4<double> getColor() const;

    private:
        Vector4<double> Color_value;

        // Sets the brush's color.
        void setColor(Vector4<double> value);

    };
}