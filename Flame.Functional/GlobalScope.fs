namespace Flame.Functional

open System
open Flame
open Flame.Compiler

/// Defines a "global scope", i.e. a scope outside function.
type GlobalScope(binder : IBinder, conversionRules : IConversionRules, log : ICompilerLog, namer : IType -> string, memberProvider : IType -> ITypeMember seq) =

    new(binder, conversionRules, log, namer : IConverter<IType, string>, memberProvider : Func<IType, ITypeMember seq>) =
        GlobalScope(binder, conversionRules, log, namer.Convert, memberProvider.Invoke)

    /// Gets this global scope's binder.
    member this.Binder =
        binder

    /// Gets all instance, static and extension
    /// type members for the given type.
    member this.GetAllMembers =
        memberProvider

    /// Gets this global scope's type namer.
    member this.TypeNamer =
        namer

    /// Gets this global scope's conversion rules.
    member this.ConversionRules = 
        conversionRules

    /// Gets this global scope's log.
    member this.Log = 
        log

    /// Gets this global scope's environment.
    member this.Environment =
        match binder with
        | null -> null
        | _    -> binder.Environment