namespace Flame.Functional

open Flame
open Flame.Compiler
open Flame.Compiler.Expressions
open Flame.Compiler.Variables
open Flame.Compiler.Statements
open System.Linq
open System.Collections.Generic

/// Defines a scope within a function.
/// Note: a local scope's variables are stored as an immutable dictionary.
type LocalScope private(parentScope : LocalScope option, funcScope : FunctionScope, locals : Map<string, IVariable>) =

    new(parentScope, funcScope) =
        LocalScope(parentScope, funcScope, Map.empty)

    new(funcScope : FunctionScope) =
        LocalScope(None, funcScope)

    new(globalScope : GlobalScope) =
        LocalScope(new FunctionScope(globalScope))

    /// Gets this local scope's parent scope.
    member this.Parent =
        parentScope

    /// Gets this local scope's associated function scope.
    member this.Function =
        funcScope

    /// Gets this local scope's associated global scope.
    member this.Global = 
        funcScope.Global

    /// Gets this local scope's locals.
    member this.Locals = 
        locals

    /// Gets a boolean value that tells if this local scope is the root local scope.
    member this.IsRoot = 
        parentScope.IsNone

    /// Finds out if this local scope declares a
    /// variable with the given name directly.
    member this.DeclaresDirectly name = 
        locals.ContainsKey name

    /// Declares a variable of the given type and name.
    member this.DeclareVariable varType name =
        let lbVar = new LateBoundVariable(name, varType) :> IVariable
        new LocalScope(parentScope, funcScope, locals |> Map.add name lbVar), lbVar

    /// Gets the variable with the given name.
    member this.GetVariable name =
        if locals.ContainsKey(name) then
            Some (locals.Item name)
        else
            match parentScope with
            | Some scope -> scope.GetVariable name
            | None       -> funcScope.GetVariable name

    /// Creates a child scope.
    member this.ChildScope =
        new LocalScope(Some this, funcScope)

    /// Creates a release statement for this local scope.
    member this.ReleaseStatement =
        new BlockStatement(locals.Select(fun item -> item.Value.CreateReleaseStatement()).ToArray())