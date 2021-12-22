namespace Castos

[<AutoOpen>]
module ErrorHandling =

    let (>>=) result func = Result.bind func result

    //let (>=>) f1 f2 = f1 >> (bind f2)