module UpcastGeneric
//type MyObj  = 
//    interface IDisposable with
//        member x.Dispose() = printfn "Disposed!"
//    override x.ToString() = "Hello World!"
//    new() = {}


//let inline myUpcast (x: ^a) (y: ^b) :  ^b =  x 
//let inline myUpcast< 'a, 'b > (x: 'a) :  'b when 'a :> 'b =  x 
//let inline myUpcast< ^a , ^b when ^a :> ^b > (x: ^a) :  ^b  =  x 

//let gobble (o:IDisposable) = printfn "%s" <| o.ToString() ; o.Dispose()
//
//gobble (new MyObj())

//let inline upcastseq (xs: seq< ^a >) : seq< ^b > = xs |> Seq.map myUpcast
//let inline upcastseq (xs: seq< ^a >) : seq< ^b > when ^a :> ^b = xs :?> seq< ^b >
    //let cast (x:^a) :^b = x :> ^b
    //in one module
//    let printEm (os: seq<obj>) = 
//        let printObj o = o.ToString() |> printfn "%s"
//        for o in os do
//            printObj o
    

    
    //in another module
    //let printit = stringEm >> Seq.toArray >> (fun sArr-> String.Join (", ",sArr)) >> printfn "%s"
    //Seq.singleton "Hello World" |> upcastseq |> printEm

