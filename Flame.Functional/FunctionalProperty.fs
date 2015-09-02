namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open Flame.Compiler.Statements
open LazyHelpers

/// Defines a functional-style property.
type FunctionalProperty(header : FunctionalMemberHeader, 
                        declType : IType,
                        isStatic : bool,
                        propertyType : Lazy<IType>, 
                        parameters : LazyArrayBuilder<IParameter>,
                        accessors : LazyApplicationArray<IProperty, IAccessor>) as this =

    inherit FunctionalMemberBase(header, declType, isStatic)

    let appliedAccessors = accessors.ApplyLazy this

    new(header : FunctionalMemberHeader, declType : IType, isStatic : bool) =        
        FunctionalProperty(header, 
                           declType, 
                           isStatic,
                           lazy (PrimitiveTypes.Void),
                           new LazyArrayBuilder<IParameter>(),
                           new LazyApplicationArray<IProperty, IAccessor>())

    /// Gets this functional-style property's return type.
    member this.PropertyType = propertyType.Value

    /// Gets this functional-style property's indexer parameters.
    member this.IndexerParameters = parameters.Value |> Seq.ofArray

    /// Gets this functional-style property's accessors, with lazy evaluation.
    member this.LazyAccessors = appliedAccessors

    /// Gets this functional-style property's accessors, with lazy evaluation.
    member this.Accessors = evalLazy appliedAccessors |> Seq.ofArray

    /// Sets this functional property's type.
    member this.WithPropertyType value =
        new FunctionalProperty(header, declType, isStatic, value, parameters, accessors)

    /// Adds this indexer parameter to this function property.
    member this.WithIndexerParameter value =
        new FunctionalProperty(header, declType, isStatic, propertyType, parameters.Append value, accessors)

    /// Adds an accessor to this functional-style property.
    member this.WithAccessor value =
        new FunctionalProperty(header, declType, isStatic, propertyType, parameters, accessors.Append value)

    interface IProperty with
        member this.PropertyType = propertyType.Value
        member this.IndexerParameters = this.IndexerParameters
        member this.Accessors = this.Accessors