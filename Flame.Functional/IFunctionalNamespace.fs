namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler

/// Defines a functional-style namespace.
type IFunctionalNamespace =
    inherit INamespaceBranch

    /// Adds a type to this functional-style namespace.
    abstract member WithType : (INamespace -> IType) -> IFunctionalNamespace

    /// Adds a child namespace to this functional-style namespace.
    abstract member WithNamespace : (INamespaceBranch -> INamespaceBranch) -> IFunctionalNamespace