namespace Flame.Functional

open Flame
open Flame.Binding
open System

type TypeName(path : string[]) =
    new (name : string) =
        TypeName(name.Split('.'))

    /// Gets this type name's "path".
    member this.Path = path

    member this.Name = String.Join(".", this.Path)

    /// Checks if this type name is empty.
    member this.IsEmpty = path.Length = 0

    /// Gets this type name's "head" element.
    member this.Head = path.[0]

    /// Gets this type name's "tail" type name.
    member this.Tail = 
        new TypeName(path |> List.ofArray
                          |> List.tail
                          |> Array.ofList)

    /// Gets this type name, without the last element.
    member this.Start =
        new TypeName(path |> Seq.take (path.Length - 1)
                          |> Array.ofSeq)

    /// Appends the given type name to this type name, and returns the result.
    member this.Append (other : TypeName) =
        new TypeName(Array.append this.Path other.Path)

    member first.Equals (second : TypeName) =
        first.Path.Length = second.Path.Length && 
        Seq.zip first.Path second.Path |> Seq.exists (fun (x, y) -> x <> y)
                                       |> not

    override first.Equals (second : obj) =
        match second with
        | :? TypeName as snd -> first.Equals snd
        | _                  -> false

    override this.GetHashCode() =
        path |> Seq.fold (fun res item -> (res <<< 1) ^^^ hash(item)) 0

    override this.ToString() = this.Name

    member first.CompareTo (second : TypeName) = 
        let longest   = max first.Path.Length second.Path.Length
        let extend (path : string[]) =
            path |> Seq.append (Seq.init (longest - path.Length) (fun _ -> ""))
        let newFirst  = extend first.Path
        let newSecond = extend second.Path
        let compare result (x : string, y) = 
            if result = 0 then x.CompareTo(y) else result
        Seq.zip newFirst newSecond |> Seq.fold compare 0

    interface IComparable<TypeName> with
        member first.CompareTo second = first.CompareTo second

    interface IComparable with
        member first.CompareTo second = 
            match second with
            | :? TypeName as snd -> first.CompareTo snd
            | _                  -> 1

/// Defines a functional binder type.
type FunctionalBinder(innerBinder : IBinder,
                      usingNamespaces : Set<TypeName>,
                      mappedNamespaces : Map<string, TypeName>,
                      aliasedTypes : Map<TypeName, Lazy<IType>>) =
    new (innerBinder : IBinder,
         usingNamespaces : Set<TypeName>,
         mappedNamespaces : Map<string, TypeName>) =
        FunctionalBinder(innerBinder, usingNamespaces, mappedNamespaces, Map.empty)

    new (innerBinder : IBinder) =
        FunctionalBinder(innerBinder, Set.singleton (new TypeName([||])), Map.empty)

    /// Uses the given namespace when resolving names.
    member this.UseNamespace (ns : TypeName) =
        new FunctionalBinder(innerBinder, usingNamespaces.Add ns, mappedNamespaces)

    /// Uses the given namespace, and all enclosing namespaces, when resolving names.
    member this.UseNamespace (ns : INamespace) =
        let fullName  = new TypeName(ns.FullName)
        let newUsings = fullName.Path |> Seq.fold (fun (name : TypeName, results) _ -> name.Start, Set.add name results) (fullName, usingNamespaces)
                                      |> snd
        new FunctionalBinder(innerBinder, newUsings, mappedNamespaces, aliasedTypes)

    /// Maps the given name to the given namespace.
    member this.MapNamespace (name : string) (ns : TypeName) =
        new FunctionalBinder(innerBinder, usingNamespaces, mappedNamespaces.Add(name, ns), aliasedTypes)

    /// Aliases the given type to the given type name.
    member this.AliasType (name : TypeName) (target : IType Lazy) =
        new FunctionalBinder(innerBinder, usingNamespaces, mappedNamespaces, aliasedTypes.Add(name, target))

    /// Gets this binder's environment.
    member this.Environment = innerBinder.Environment

    /// Binds the given name to a type.
    member this.Bind (name : TypeName) : IType = 
        if name.IsEmpty then
            null
        else if aliasedTypes.ContainsKey name then
            aliasedTypes.[name].Value
        else if mappedNamespaces.ContainsKey name.Head then
            mappedNamespaces.[name.Head].Append name.Tail |> this.Bind
        else
            let tyMatch = usingNamespaces |> Seq.map (fun x -> x.Append name)
                                          |> Seq.map (fun x -> innerBinder.BindType x.Name)
                                          |> Seq.tryFind ((<>) null)
            match tyMatch with
            | None   -> null
            | Some x -> x

    /// Binds the given name to a type.
    member this.Bind (name : string) : IType =
        this.Bind (new TypeName(name))

    interface IBinder with
        member this.BindType name = this.Bind name
        member this.Environment = innerBinder.Environment
        member this.GetTypes()  = innerBinder.GetTypes()