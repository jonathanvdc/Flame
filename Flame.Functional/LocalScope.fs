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
type LocalScope private(parentScope : LocalScope option, funcScope : FunctionScope, locals : Map<string, IVariable>, tag : UniqueTag) =

    new(parentScope, funcScope, tag : UniqueTag) =
        LocalScope(parentScope, funcScope, Map.empty, tag)

    new(funcScope : FunctionScope) =
        LocalScope(funcScope, null)

    new(funcScope : FunctionScope, tag : UniqueTag) =
        LocalScope(None, funcScope, tag)

    new(globalScope : GlobalScope, tag : UniqueTag) =
        LocalScope(new FunctionScope(globalScope), tag)

    new(globalScope : GlobalScope) =
        LocalScope(globalScope, null)

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
        
    /// Gets the enclosing control flow block's tag.
    member this.ControlTag =
        tag

    /// Gets a boolean value that tells if this local scope is the root local scope.
    member this.IsRoot = 
        parentScope.IsNone

    /// Gets all local variables and parameters
    /// that belong to this local scope and
    /// its parents.
    member this.AllLocals : Map<string, IVariable> =
        let parentLocals = 
            match parentScope with
            | Some scope -> 
                scope.AllLocals
            | None ->
                funcScope.Parameters
        Map.fold (fun result k v -> Map.add k v result) parentLocals locals

    /// Finds out if this local scope declares a
    /// variable with the given name directly.
    member this.DeclaresDirectly (name : string) : bool = 
        locals.ContainsKey name

    /// Registers the given variable with this
    /// local scope under the given name.
    member this.WithVariable (variable : IVariable) (name : string) : LocalScope =
        new LocalScope(parentScope, funcScope, locals |> Map.add name variable, tag)

    /// Declares a variable of the given type and name.
    member this.DeclareVariable (varType : IType) (name : string) : LocalScope * IVariable =
        let lbVar = LocalVariable(name, varType) :> IVariable
        this.WithVariable lbVar name, lbVar

    /// Gets the variable with the given name.
    member this.GetVariable (name : string) : IVariable option =
        if locals.ContainsKey(name) then
            Some (locals.Item name)
        else
            match parentScope with
            | Some scope -> scope.GetVariable name
            | None       -> funcScope.GetVariable name

    /// Creates a child scope.
    member this.ChildScope =
        new LocalScope(Some this, funcScope, tag)

    /// Creates a child scope with the given control flow tag.
    member this.FlowChildScope flowTag =
        new LocalScope(Some this, funcScope, flowTag)

    /// Creates a release statement for this local scope.
    member this.ReleaseStatement =
        new BlockStatement(locals.Select(fun item -> item.Value.CreateReleaseStatement()).ToArray())