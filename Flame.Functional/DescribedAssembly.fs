namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open System

/// A lazy namespace binder that performs a root namespace lookup
/// every time a type is bound or requested.
type LazyNamespaceBinder(env : IEnvironment, getNamespace : unit -> INamespaceTree) =
    interface IBinder with
        member this.Environment = env
        member this.GetTypes() = getNamespace().GetAllTypes() :> seq<IType>
        member this.BindType name = getNamespace().FindType name

/// Defines a simple described assembly type.
/// This type is mutable such that we can first define the assembly's type hierarchy without
/// evaluating anything, then perform a bait-and-switch on this assembly's main namespace, 
/// and some time after that evaluate all associated types.
type DescribedAssembly(name : string, env : IEnvironment) as this =

    member this.Name = name
    member this.Environment = env

    member val Version = new Version() with get, set
    member val EntryPoint = null with get, set 
    member val Attributes = Seq.empty with get, set
    member val MainNamespace = new FunctionalNamespace(new FunctionalMemberHeader(null), this) :> INamespaceBranch with get, set

    member this.Binder = new LazyNamespaceBinder(env, fun () -> this.MainNamespace :> INamespaceTree) :> IBinder

    interface IAssembly with
        member this.Name = name
        member this.FullName = name
        member this.GetAttributes() = this.Attributes

        member this.AssemblyVersion = this.Version
        member this.CreateBinder() = this.Binder
        member this.GetEntryPoint() = this.EntryPoint