namespace Flame.Functional

open Flame
open Flame.Compiler

/// Defines a "global scope", i.e. a scope outside function.
type GlobalScope(binder : IBinder, conversionRules : IConversionRules, log : ICompilerLog, namer : IType -> string) =

    new(binder, conversionRules, log, namer : IConverter<IType, string>) =
        GlobalScope(binder, conversionRules, log, namer.Convert)

    /// Gets this global scope's binder.
    member this.Binder =
        binder

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