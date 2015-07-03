namespace Flame.Functional

open System.Linq
open LazyHelpers

/// A type that is dedicated to building a lazy array, one
/// array item at a time. This type is purely functional:
/// it does not contain any mutable state.
type LazyApplicationArray<'a, 'b> private(valueList : ('a -> 'b) list) =
    new() =
        LazyApplicationArray([])
    new(items : seq<'b>) =
        LazyApplicationArray(items |> Seq.map (fun x -> fun _ -> x)
                                   |> Enumerable.Reverse
                                   |> List.ofSeq)

    /// Appends an item to the array.
    member this.Append item =
        new LazyApplicationArray<'a, 'b>(item :: valueList)

    member this.ApplyLazy arg =
        lazy (valueList |> Enumerable.Reverse
                        |> Seq.map (fun f -> f arg)
                        |> Seq.toArray)