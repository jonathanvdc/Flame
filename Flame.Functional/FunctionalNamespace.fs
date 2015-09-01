namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open LazyHelpers

/// Defines a functional-style namespace.
type FunctionalNamespace private(header : FunctionalMemberHeader, declAsm : IAssembly,
                                 childNamespaces : LazyApplicationArray<INamespaceBranch, INamespaceBranch>,
                                 types : LazyApplicationArray<INamespace, IType>) as this =

    let appliedNs = childNamespaces.ApplyLazy this
    let appliedTypes = types.ApplyLazy this

    new(header, declAsm) =
        FunctionalNamespace(header, declAsm, 
                            new LazyApplicationArray<INamespaceBranch, INamespaceBranch>(),
                            new LazyApplicationArray<INamespace, IType>())

    /// Gets this functional-style namespace's declaring assembly.
    member this.DeclaringAssembly = declAsm

    /// Gets this functional-style namespace's name.
    member this.Name = header.Name

    /// Gets all child namespaces this functional-style namespace contains directly.
    member this.Namespaces = evalLazy appliedNs

    /// Gets all types this functional-style namespace contains directly.
    member this.Types = evalLazy appliedTypes

    /// Sets this functional-style field's field type.
    member this.WithNamespace value =
        new FunctionalNamespace(header, declAsm, childNamespaces.Append value, types)

    /// Sets this functional-style field's initial value.
    member this.WithType value =
        new FunctionalNamespace(header, declAsm, childNamespaces, types.Append value)

    interface INamespace with
        member this.DeclaringAssembly = declAsm
        member this.GetTypes() = this.Types

    interface IFunctionalNamespace with
        member this.WithType value =
            (this.WithType value) :> IFunctionalNamespace

        member this.WithNamespace value =
            (this.WithNamespace value) :> IFunctionalNamespace

    interface INamespaceBranch with
        member this.GetNamespaces() = Seq.ofArray this.Namespaces

    interface IMember with
        member this.Name = header.Name
        member this.FullName = header.Name
        member this.Attributes = header.Attributes