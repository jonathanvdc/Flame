namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open Flame.Compiler.Build
open Flame.Compiler.Statements
open LazyHelpers

/// Defines a functional-style method.
type FunctionalMethod private(header : FunctionalMemberHeader, declType : IType,
                              isStatic : bool, isCtor : bool,
                              genericParameters : IGenericMember -> IGenericParameter seq,
                              baseMethods : IMethod -> IMethod seq,
                              returnType : IMethod -> IType, 
                              parameters : IMethod -> IParameter seq,
                              body : IMethod -> IStatement) as this =

    inherit FunctionalMemberBase(header, declType, isStatic)

    let appliedGenericParams = lazy genericParameters this
    let appliedBaseMethods = lazy (baseMethods this |> Seq.toArray)
    let appliedBody = lazy body this
    let appliedRetType = lazy returnType this
    let appliedParameters = lazy (parameters this |> Seq.toArray)

    new(header : FunctionalMemberHeader, declType : IType,
        isStatic : bool) =
        FunctionalMethod(header, declType, isStatic, false, 
                         (fun _ -> Seq.empty),
                         (fun _ -> Seq.empty),
                         (fun _ -> PrimitiveTypes.Void),
                         (fun _ -> Seq.empty),
                         (fun _ -> null))

    /// Gets this functional-style method's return type.
    member this.ReturnType = appliedRetType.Value

    /// Finds out if this functional-style method is a constructor or not.
    member this.IsConstructor = isCtor

    /// Gets this functional-style method's generic parameters.
    member this.GenericParameters = evalLazy appliedGenericParams

    /// Gets this functional-style method's parameters.
    member this.Parameters = appliedParameters.Value

    /// Gets this functional-style method's body statement, with lazy evaluation.
    member this.LazyBody = appliedBody

    /// Gets this functional-style method's body statement.
    member this.Body = evalLazy appliedBody

    /// Gets this functional-style method's body-generating function.
    member this.CreateBody = body

    /// Gets this functional-style method's base methods, 
    /// with a lazy evaluation scheme.
    member this.LazyBaseMethods = appliedBaseMethods

    /// Gets this functional-style method's base methods.
    member this.BaseMethods = appliedBaseMethods.Value

    /// Gets this functional method as a constructor.
    member this.AsConstructor =
        new FunctionalMethod(header, declType, isStatic, true, genericParameters, baseMethods, returnType, parameters, body)

    /// Gets this functional method as a non-constructor method.
    member this.AsMethod = 
        new FunctionalMethod(header, declType, isStatic, false, genericParameters, baseMethods, returnType, parameters, body)

    /// Sets this functional method's return type.
    member this.WithReturnType value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters, baseMethods, value, parameters, body)

    /// Sets this functional method's generic parameters.
    member this.WithGenericParameters value =
        new FunctionalMethod(header, declType, isStatic, isCtor, value, baseMethods, returnType, parameters, body)

    /// Sets this functional method's parameters.
    member this.WithParameters value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters, baseMethods, returnType, value, body)

    /// Sets this functional-style method's body.
    member this.WithBody value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters, baseMethods, returnType, parameters, value)

    /// Sets this functional-style method's base method function.
    member this.WithBaseMethods value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters, value, returnType, parameters, body)

    interface IMethod with
        member this.ReturnType = this.ReturnType
        member this.GetParameters() = this.Parameters
        member this.IsConstructor = isCtor
        member this.GetBaseMethods() = this.BaseMethods

        member this.Invoke(target : IBoundObject, args : IBoundObject seq) = null // We don't do that yet.

        member this.GetGenericParameters() = this.GenericParameters
        member this.GetGenericArguments() = Seq.empty
        member this.GetGenericDeclaration() = this :> IMethod
        member this.MakeGenericMethod tArgs = new DescribedGenericMethodInstance(this :> IMethod, new EmptyGenericResolver(), this.DeclaringType, tArgs) :> IMethod

    interface IBodyMethod with
        member this.GetMethodBody() = this.Body
        