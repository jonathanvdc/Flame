#pragma once
#include <memory>
#include "ICommand.h"
#include "IRenderTarget.h"
#include "IBrush.h"

namespace Engine
{
    // Describes a shape: an object that can draw itself on a render target.
    struct IShape
    {
        // Creates a render command for this shape.
        virtual std::shared_ptr<ICommand<std::shared_ptr<IRenderTarget>>> CreateCommand(std::shared_ptr<IBrush> Brush) const = 0;

    };
}