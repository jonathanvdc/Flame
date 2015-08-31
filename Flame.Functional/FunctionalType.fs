namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open Flame.Compiler.Build
open LazyHelpers

/// A header for function members.
type FunctionalMemberHeader(name : string, attrs : IAttribute LazyArrayBuilder, srcLocation : SourceLocation) =
    new(name) =
        FunctionalMemberHeader(name, new LazyArrayBuilder<IAttribute>(), null)
    new(name, attrs : seq<IAttribute>) =
        FunctionalMemberHeader(name, attrs, null)
    new(name, attrs : seq<IAttribute>, srcLocation : SourceLocation) =
        FunctionalMemberHeader(name, new LazyArrayBuilder<IAttribute>(attrs), srcLocation)

    /// Gets this functional-style member header's name.
    member this.Name = name
    /// Gets this functional-style member header's attributes.
    member this.Attributes = Seq.ofArray attrs.Value

    member this.Location = srcLocation

    /// Adds an attribute to this functional-style member header.
    member this.WithAttribute value =
        new FunctionalMemberHeader(name, attrs.Append value, srcLocation)

    member this.WithLocation value =
        new FunctionalMemberHeader(name, attrs, value)

/// Provides common functionality for objects that support
/// constructing types in a functional fashion.
type FunctionalType private(header : FunctionalMemberHeader, declNs : INamespace,
                            baseTypes : IType -> IType seq, 
                            nestedTypes : LazyApplicationArray<INamespace, IType>, 
                            genericParams : IGenericMember -> IGenericParameter seq,
                            methods : LazyApplicationArray<IType, IMethod>, properties : LazyApplicationArray<IType, IProperty>, 
                            fields : LazyApplicationArray<IType, IField>, ns : LazyApplicationArray<INamespaceBranch, INamespaceBranch>) as this =

    let appliedNestedTypes = nestedTypes.ApplyLazy this
    let appliedGenericParams = lazy genericParams this
    let appliedMethods = methods.ApplyLazy this
    let appliedProperties = properties.ApplyLazy this
    let appliedFields = fields.ApplyLazy this
    let appliedBaseTypes = lazy (baseTypes this |> Array.ofSeq)
    let appliedNs = ns.ApplyLazy this
    let ctorPartition = appliedMethods |~ Array.partition (fun x -> x.IsConstructor)

    new(header, declNs) =
        FunctionalType(header, declNs,
                       (fun _ -> Seq.empty), new LazyApplicationArray<INamespace, IType>(),
                       (fun _ -> Seq.empty),
                       new LazyApplicationArray<IType, IMethod>(), new LazyApplicationArray<IType, IProperty>(),
                       new LazyApplicationArray<IType, IField>(), new LazyApplicationArray<INamespaceBranch, INamespaceBranch>())
    new(name, declNs) =
        FunctionalType(new FunctionalMemberHeader(name), declNs)
    
    /// Gets the type's "formal" name, i.e., the header's name with a suffix that
    /// identifies the number of type parameters the type takes.
    member this.Name = 
        match Seq.length this.GenericParameters with
        | 0 -> header.Name
        | x -> header.Name + "<" + (new System.String(',', x - 1)) + ">"

    /// Gets this functional type's base types.
    member this.BaseTypes         = appliedBaseTypes.Value
    /// Gets this functional type's nested types.
    member this.NestedTypes       = evalLazy appliedNestedTypes
    /// Gets this functional type's generic parameters.
    member this.GenericParameters = evalLazy appliedGenericParams
    /// Gets this functional type's methods.
    member this.Methods           = snd ctorPartition.Value
    /// Gets this functional type's constructors.
    member this.Constructors      = fst ctorPartition.Value
    /// Gets this functional type's properties.
    member this.Properties        = evalLazy appliedProperties
    /// Gets this functional type's fields.
    member this.Fields            = evalLazy appliedFields
    /// Gets this functional type's members.
    member this.Members           = Seq.concat [ Seq.cast<ITypeMember> appliedMethods.Value; Seq.cast<ITypeMember> this.Properties; Seq.cast<ITypeMember> this.Fields ]
                                               |> Array.ofSeq
    member this.Location          = header.Location

    /// Gets this functional type's nested namespaces.
    member this.NestedNamespaces  = evalLazy appliedNs

    /// Sets this type's header.
    member this.WithHeader newHeader = 
        new FunctionalType(newHeader, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields, ns)

    /// Adds an attribute to this functional-style type.
    member this.WithAttribute attr =
        new FunctionalType(header.WithAttribute attr, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields, ns)

    /// Sets this this functional-style type's base types.
    member this.WithBaseTypes baseTypes =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields, ns)

    /// Adds a nested type to this functional-style type.
    member this.WithNestedType nestedType =
        new FunctionalType(header, declNs, baseTypes, nestedTypes.Append nestedType, genericParams, methods, properties, fields, ns)

    /// Adds a nested namespace to this functional-style type.
    member this.WithNestedNamespace value =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields, ns.Append value)

    /// Sets this functional-style type's generic parameters.
    member this.WithGenericParameters value =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, value, methods, properties, fields, ns)

    /// Adds a method to this functional-style type.
    member this.WithMethod value =
        let newMethods = methods.Append value
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, newMethods, properties, fields, ns)

    /// Adds a property to this functional-style type.
    member this.WithProperty value =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, methods, properties.Append value, fields, ns)

    /// Adds a field to this functional-style type.
    member this.WithField value =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields.Append value, ns)

    interface IFunctionalNamespace with
        /// Adds a nested type to this functional-style type.
        member this.WithType func =
            (this.WithNestedType func) :> IFunctionalNamespace

        member this.WithNamespace value =
            (this.WithNestedNamespace value) :> IFunctionalNamespace

    interface IMember with
        member this.Name = this.Name
        member this.FullName = MemberExtensions.CombineNames(declNs.FullName, this.Name)
        member this.GetAttributes() = header.Attributes

    interface IType with
        member this.AsContainerType() = null
        member this.IsContainerType = false
        member this.DeclaringNamespace = declNs
        
        member this.MakeArrayType rank = new DescribedArrayType(this :> IType, rank) :> IArrayType
        member this.MakePointerType kind = new DescribedPointerType(this :> IType, kind) :> IPointerType
        member this.MakeVectorType dims = new DescribedVectorType(this :> IType, dims) :> IVectorType

        member this.GetGenericDeclaration() = this :> IType
        member this.MakeGenericType tArgs = new DescribedGenericTypeInstance(this :> IType, tArgs) :> IType
        member this.GetGenericArguments() = Seq.empty
        member this.GenericParameters = this.GenericParameters

        member this.GetBaseTypes() = this.BaseTypes
        member this.GetMethods() = this.Methods
        member this.GetConstructors() = this.Constructors
        member this.GetProperties() = this.Properties
        member this.GetFields() = this.Fields
        member this.GetMembers() = this.Members

        member this.GetDefaultValue() = null

    interface INamespace with
        member this.DeclaringAssembly = declNs.DeclaringAssembly
        member this.GetTypes() = this.NestedTypes

    interface INamespaceBranch with
        member this.GetNamespaces() = Seq.ofArray this.NestedNamespaces

    interface ISourceMember with
        member this.Location = this.Location

/// Defines a base class for functional-style members.
[<AbstractClass>]
type FunctionalMemberBase(header : FunctionalMemberHeader, declType : IType,
                          isStatic : bool) =

    /// Gets this functional-style member's declaring type.
    member this.DeclaringType  = declType

    /// Figures out whether this functional-style member is static.
    member this.IsStatic       = isStatic

    member this.Location       = header.Location

    interface IMember with
        member this.Name = header.Name
        member this.FullName = MemberExtensions.CombineNames(declType.FullName, header.Name)
        member this.GetAttributes() = header.Attributes

    interface ITypeMember with
        member this.DeclaringType = declType
        member this.IsStatic = isStatic

    interface ISourceMember with
        member this.Location = this.Location