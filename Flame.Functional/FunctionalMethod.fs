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
                              genericParameters : LazyApplicationArray<IGenericMember, IGenericParameter>,
                              baseMethods : IMethod LazyArrayBuilder,
                              returnType : IType Lazy, 
                              parameters : IParameter LazyArrayBuilder,
                              body : IMethod -> IStatement) as this =

    inherit FunctionalMemberBase(header, declType, isStatic)

    let appliedGenericParams = genericParameters.ApplyLazy this
    let appliedBody = lazy (body this)

    new(header : FunctionalMemberHeader, declType : IType,
        isStatic : bool) =
        FunctionalMethod(header, declType, isStatic, false, 
                         new LazyApplicationArray<IGenericMember, IGenericParameter>(),
                         new LazyArrayBuilder<IMethod>(),
                         new Lazy<IType>(fun _ -> PrimitiveTypes.Void),
                         new LazyArrayBuilder<IParameter>(),
                         (fun _ -> null))

    /// Gets this functional-style method's return type.
    member this.ReturnType = returnType.Value

    /// Finds out if this functional-style method is a constructor or not.
    member this.IsConstructor = isCtor

    /// Gets this functional-style method's generic parameters.
    member this.GenericParameters = evalLazy appliedGenericParams

    /// Gets this functional-style method's parameters.
    member this.Parameters = parameters.Value

    /// Gets this functional-style method's body statement, with lazy evaluation.
    member this.LazyBody = appliedBody

    /// Gets this functional-style method's body statement.
    member this.Body = evalLazy appliedBody

    /// Gets this functional-style method's body-generating function.
    member this.CreateBody = body

    /// Gets this functional-style method's base methods.
    member this.BaseMethods = baseMethods.Value

    /// Gets this functional method as a constructor.
    member this.AsConstructor =
        new FunctionalMethod(header, declType, isStatic, true, genericParameters, baseMethods, returnType, parameters, body)

    /// Gets this functional method as a non-constructor method.
    member this.AsMethod = 
        new FunctionalMethod(header, declType, isStatic, false, genericParameters, baseMethods, returnType, parameters, body)

    /// Sets this functional method's return type.
    member this.WithReturnType value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters, baseMethods, value, parameters, body)

    /// Adds this generic parameter to this function method.
    member this.WithGenericParameter value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters.Append value, baseMethods, returnType, parameters, body)

    /// Adds this parameter to this functional method.
    member this.WithParameter value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters, baseMethods, returnType, parameters.Append value, body)

    /// Sets this functional-style method's body.
    member this.WithBody value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters, baseMethods, returnType, parameters, value)

    /// Adds a base method to this functional-style method.
    member this.WithBaseMethod value =
        new FunctionalMethod(header, declType, isStatic, isCtor, genericParameters, baseMethods.Append value, returnType, parameters, body)

    interface IMethod with
        member this.ReturnType = returnType.Value
        member this.GetParameters() = this.Parameters
        member this.IsConstructor = isCtor
        member this.GetBaseMethods() = this.BaseMethods

        member this.Invoke(target : IBoundObject, args : IBoundObject seq) = null // We don't do that yet.

        member this.GetGenericParameters() = Seq.ofArray this.GenericParameters
        member this.GetGenericArguments() = Seq.empty
        member this.GetGenericDeclaration() = this :> IMethod
        member this.MakeGenericMethod tArgs = new DescribedGenericMethodInstance(this :> IMethod, new EmptyGenericResolver(), this.DeclaringType, tArgs) :> IMethod

    interface IBodyMethod with
        member this.GetMethodBody() = this.Body
        