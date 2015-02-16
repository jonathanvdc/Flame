#pragma once

namespace Engine
{
    // Describes an action that can be applied to a target of type 'T'.
    template<typename T>
    struct ICommand
    {
        // Executes the command on the given target.
        virtual void Execute(T Target) = 0;

    };
}