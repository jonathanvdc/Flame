namespace Flame.Functional

open Flame
open System
open System.Linq

/// Provides helper functions for members.
module MemberHelpers =

    /// Gets the finite set of upper bounds for the given finite set,
    /// based on the given less-than function.
    let UpperBounds (is_lt : 'a -> 'a -> bool) (values : 'a seq) : 'a seq = 
        SetExtensions.UpperBounds(values, new Func<'a, 'a, bool>(is_lt))

    /// Selects all items that are of the given result type.
    let OfType<'Source, 'Result> (values : seq<'Source>) : 'Result seq =
        Enumerable.OfType<'Result> values

    /// Tells if the first method method implements the second.
    let IsImplementationOf (first : #IMethod) (right : #IMethod) = 
        first.IsImplementationOf right

    /// Gets all members defined by the given type's base types for the given member-getting function.
    let GetAllBaseMembers<'a when 'a : equality> (getAllMembers : IType -> 'a seq) (target : IType) =
        target.GetBaseTypes() |> Seq.map getAllMembers
                              |> Seq.concat
                              |> Seq.distinct

    /// Infers the given method's base methods for the given method-getting function.
    let InferBaseMethods (getAllMethods : IType -> IMethod seq) (target : IMethod) =
        GetAllBaseMembers getAllMethods target.DeclaringType 
            |> Seq.filter (fun x -> x.HasSameSignature target)
            |> UpperBounds IsImplementationOf

    /// Infers the given property's potential base properties for the given property-getting function.
    let InferBaseProperties (getAllProperties : IType -> IProperty seq) (target : IProperty) =
        GetAllBaseMembers getAllProperties target.DeclaringType 
            |> Seq.filter (fun x -> x.HasSameSignature target)

    /// Selects the given accessor's base accessors from a sequence of potential base properties.
    let FilterBaseAccessors (baseProps : IProperty seq) (target : IAccessor) =
        baseProps |> Seq.map (fun x -> x.GetAccessors())
                  |> Seq.concat
                  |> Seq.filter (fun x -> x.AccessorType = target.AccessorType)
                  |> UpperBounds IsImplementationOf

    /// Infers the given accesor's base accessors based on the given property-getting function.
    let InferBaseAccessors (getAllProperties : IType -> IProperty seq) (target : IAccessor) = 
        let baseProps = InferBaseProperties getAllProperties target.DeclaringProperty
        FilterBaseAccessors baseProps target