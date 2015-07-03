namespace Flame.Functional

open System.Linq

module LazyHelpers =
    // "force-evaluate lazy" function
    let evalLazy<'a> (arg : 'a Lazy) = arg.Value

    // "lazy forward pipe" operator
    let (|~) arg func = lazy (evalLazy arg |> func)

    let concatLazy<'a> (vals : 'a Lazy seq) =
        lazy (Seq.map evalLazy vals)

    let mapLazy<'a, 'b> (mapping : 'a -> 'b) (vals : 'a seq Lazy) =
        vals |~ Seq.map mapping

    let toLazyArray<'a> (vals : 'a seq Lazy) =
        vals |~ Array.ofSeq

open LazyHelpers

/// A type that is dedicated to building a lazy array, one
/// array item at a time. This type is purely functional:
/// it does not contain any mutable state.
type LazyArrayBuilder<'a> private(valueList : 'a Lazy list, lazyArr : 'a[] Lazy) =
    private new(valueList) =
        LazyArrayBuilder(valueList, valueList |> Enumerable.Reverse 
                                              |> concatLazy
                                              |> toLazyArray)
    new() =
        LazyArrayBuilder([], lazy [||])
    new(items : seq<'a>) =
        LazyArrayBuilder(items |> Seq.map (fun x -> new Lazy<'a>(fun _ -> x))
                               |> Enumerable.Reverse
                               |> List.ofSeq)

    /// Appends an item to the array.
    member this.Append item =
        new LazyArrayBuilder<'a>(item :: valueList)

    member this.Concat (other : LazyArrayBuilder<'a>) =
        new LazyArrayBuilder<'a>(List.append other.ListOfLazy valueList)

    /// Gets a list of lazy objects.
    member private this.ListOfLazy =
        valueList

    /// Gets the lazy array.
    member this.LazyArray =
        lazyArr

    /// Gets the lazy array's value.
    member this.Value =
        lazyArr.Value

    /// Finds out if this lazy array has already been created.
    member this.IsValueCreated =
        lazyArr.IsValueCreated