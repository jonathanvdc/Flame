namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open Flame.Compiler.Statements
open LazyHelpers

/// Defines a functional-style accessor.
type FunctionalAccessor private(header : FunctionalMemberHeader,
                                declProp : IProperty,
                                accType : AccessorType,
                                baseMethods : IMethod LazyArrayBuilder,
                                returnType : IType Lazy, 
                                parameters : IParameter LazyArrayBuilder,
                                body : IMethod -> IStatement) as this =

    inherit FunctionalMemberBase(header, declProp.DeclaringType, declProp.IsStatic)

    let appliedBody = lazy (body this)

    new(header : FunctionalMemberHeader, declProp : IProperty, accType : AccessorType) =
        let retType = if accType.Equals(AccessorType.GetAccessor) then 
                          lazy (declProp.PropertyType) 
                      else 
                          lazy (PrimitiveTypes.Void)
        FunctionalAccessor(header, declProp, accType,
                           new LazyArrayBuilder<IMethod>(),
                           retType,
                           new LazyArrayBuilder<IParameter>(),
                           fun _ -> null)

    /// Gets this functional-style accessor's return type.
    member this.ReturnType = returnType.Value

    /// Gets this functional-style accessor's property type.
    member this.DeclaringProperty = declProp

    /// Gets this functional-style accessor's accessor type.
    member this.AccessorType = accType

    /// Gets this functional-style accessor's parameters.
    member this.Parameters = parameters.Value

    /// Gets this functional-style accessor's body statement, with lazy evaluation.
    member this.LazyBody = appliedBody

    /// Gets this functional-style accessor's body statement.
    member this.Body = evalLazy appliedBody

    /// Gets this functional-style accessor's base methods.
    member this.BaseMethods = baseMethods.Value

    /// Sets this functional accessor's return type.
    member this.WithReturnType value =
        new FunctionalAccessor(header, declProp, accType, baseMethods, value, parameters, body)

    /// Adds this parameter to this functional accessor.
    member this.WithParameter value =
        new FunctionalAccessor(header, declProp, accType, baseMethods, returnType, parameters.Append value, body)

    /// Sets this functional-style method's body.
    member this.WithBody value =
        new FunctionalAccessor(header, declProp, accType, baseMethods, returnType, parameters, value)

    /// Adds a base method to this functional-style method.
    member this.WithBaseMethod value =
        new FunctionalAccessor(header, declProp, accType, baseMethods.Append value, returnType, parameters, body)

    interface IAccessor with
        member this.AccessorType = this.AccessorType
        member this.DeclaringProperty = this.DeclaringProperty

    interface IMethod with
        member this.ReturnType = this.ReturnType
        member this.GetParameters() = this.Parameters
        member this.IsConstructor = false
        member this.GetBaseMethods() = this.BaseMethods

        member this.Invoke(target : IBoundObject, args : IBoundObject seq) = null // We don't do that yet.

        member this.GetGenericParameters() = Seq.empty
        member this.GetGenericArguments() = Seq.empty
        member this.GetGenericDeclaration() = this :> IMethod
        member this.MakeGenericMethod tArgs = this :> IMethod

    interface IBodyMethod with
        member this.GetMethodBody() = this.Body