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

    /// Gets this type name's "head".
    member this.Head = path.[0]

    /// Gets this type name's "tail" type name.
    member this.Tail = 
        new TypeName(path |> List.ofArray
                          |> List.tail
                          |> Array.ofList)

    /// Appends the given type name to this type name, and returns the result.
    member this.Append (other : TypeName) =
        new TypeName(Array.append this.Path other.Path)

    /// Gets all possible partitions for this type name.
    member this.AllPartitions =
        let _, _, output = path |> Seq.fold (fun (fstList, sndList, results) _ -> 
                                                 List.append fstList [List.head sndList], 
                                                 List.tail sndList, 
                                                 List.append results [new TypeName(Array.ofList fstList), new TypeName(Array.ofList sndList)]) 
                                            (List.empty, List.ofArray path, [])
        output

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
        Seq.zip first.Path second.Path |> Seq.fold (fun result (x, y) -> if result = 0 then x.CompareTo(y) else result) 0

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
                      mappedNamespaces : Map<string, TypeName>) =
    new (innerBinder : IBinder) =
        FunctionalBinder(innerBinder, Set.empty, Map.empty)

    /// Uses the given namespace.
    member this.UseNamespace (ns : TypeName) =
        new FunctionalBinder(innerBinder, usingNamespaces.Add ns, mappedNamespaces)

    /// Maps the given name to the given namespace.
    member this.MapNamespace (name : string) (ns : TypeName) =
        new FunctionalBinder(innerBinder, usingNamespaces, mappedNamespaces.Add(name, ns))

    /// Gets this binder's environment.
    member this.Environment = innerBinder.Environment

    /// Binds the given name to a type.
    member this.Bind (name : TypeName) : IType = 
        if name.IsEmpty then
            null
        else if mappedNamespaces.ContainsKey name.Head then
            mappedNamespaces.[name.Head].Append name.Tail |> this.Bind
        else
            let matches = name.AllPartitions |> Seq.filter (fun (_, y) -> not(y.IsEmpty))
                                             |> Seq.filter (fun (x, _) -> x.IsEmpty || usingNamespaces.Contains x)
                                             |> Seq.map (fun (_, x) -> innerBinder.BindType x.Name)
                                             |> Array.ofSeq
            if matches.Length = 0 then
                null
            else
                matches.[0]

    /// Binds the given name to a type.
    member this.Bind (name : string) : IType =
        this.Bind (new TypeName(name))

    interface IBinder with
        member this.BindType name = this.Bind name
        member this.Environment = innerBinder.Environment
        member this.GetTypes()  = innerBinder.GetTypes()