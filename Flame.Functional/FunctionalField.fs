namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open LazyHelpers

/// Defines a functional-style field that is possibly initialized.
type FunctionalField(header : FunctionalMemberHeader, declType : IType,
                     isStatic : bool, fieldType : IType Lazy, 
                     initValue : IExpression Lazy) =

    inherit FunctionalMemberBase(header, declType, isStatic)

    new(header, declType, isStatic, fieldType) =
        FunctionalField(header, declType, isStatic, fieldType, null)

    /// Gets this functional-style field's field type.
    member this.FieldType     = fieldType.Value

    /// Gets this functional-style field's initial value.
    member this.InitialValue  = initValue.Value

    /// Sets this functional-style field's field type.
    member this.WithFieldType value =
        new FunctionalField(header, declType, isStatic, value, initValue)

    /// Sets this functional-style field's initial value.
    member this.WithInitialValue init =
        new FunctionalField(header, declType, isStatic, fieldType, init)

    interface IField with
        member this.FieldType = this.FieldType

    interface IInitializedField with
        member this.GetValue() = this.InitialValue