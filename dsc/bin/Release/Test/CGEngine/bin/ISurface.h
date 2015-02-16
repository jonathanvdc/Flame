#pragma once

namespace Engine
{
    // Describes a surface: an object that specifies its own dimensions.
    struct ISurface
    {
        // Gets the surface's width.
        virtual double getWidth() const = 0;

        // Gets the surface's height.
        virtual double getHeight() const = 0;

    };
}