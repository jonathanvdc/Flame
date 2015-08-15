﻿namespace Flame.Functional

open System
open System.Collections.Generic
open Flame
open Flame.Compiler

/// Defines a "global scope", i.e. a scope outside function.
type GlobalScope(binder : FunctionalBinder, 
                 conversionRules : IConversionRules, 
                 log : ICompilerLog, 
                 namer : IType -> string, 
                 memberProvider : IType -> ITypeMember seq,
                 getParameters : IMethod option -> Map<string, IVariable>) =

    new(binder, conversionRules, log, namer : IConverter<IType, string>, memberProvider : Func<IType, ITypeMember seq>, getParameters : Func<IMethod, IReadOnlyDictionary<string, IVariable>>) =
        let getParams x = 
            let z = match x with 
                    | None   -> null 
                    | Some y -> y
            getParameters.Invoke z |> Seq.map (fun pair -> pair.Key, pair.Value)
                                   |> Map.ofSeq
        GlobalScope(binder, conversionRules, log, namer.Convert, memberProvider.Invoke, getParams)

    /// Gets this global scope's binder.
    member this.Binder =
        binder

    /// Creates a new global scope with the given binder.
    member this.WithBinder value =
        new GlobalScope(value, conversionRules, log, namer, memberProvider, getParameters)

    /// Gets the given method's parameters, as a map of names mapped to variables.
    member this.GetParameters =
        getParameters

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
    member this.Environment = binder.Environment