namespace Flame.Functional

open Flame

type FunctionConverter<'a, 'b>(func : 'a -> 'b) =
    member this.Function = func

    interface IConverter<'a, 'b> with
        member this.Convert value =
            this.Function value
    

