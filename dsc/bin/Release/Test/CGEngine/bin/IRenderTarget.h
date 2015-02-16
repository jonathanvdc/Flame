#pragma once
#include "IRenderContext.h"
#include "ISurface.h"

namespace Engine
{
    // Describes a render target, which is a surface that can be drawn to.
    struct IRenderTarget : public ISurface
    {
        // Gets the render target's render context, which supports the creation of shapes to draw on this render target.
        virtual std::shared_ptr<IRenderContext> getContext() const = 0;

    };
}