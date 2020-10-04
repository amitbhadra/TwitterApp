// Learn more about F# at http://fsharp.org

#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

#time "on"

// #Actor Implementation
// An Actor is more lightweight than a thread. Millions of actors can be generated in Akka,
// the secret is that an Actor can reuse a thread.
//
// The mapping relationship between an Actor and a Thread is decided by a Dispatcher.
// 
// This example creates 10 Actors, and prints its thread name when invoked.
//
// You will find there is no fixed mapping relationship between Actors and Threads. 
// An Actor can use many threads. And a thread can be used by many Actors.


//module Akka = 


    // #Using Actor
    // Actors are one of Akka's concurrent models.
    // An Actor is a like a thread instance with a mailbox. 
    // It can be created with system.ActorOf: use receive to get a message, and <! to send a message.
    // This example is an EchoServer which can receive messages then print them.


let system = System.create "system" (Configuration.defaultConfig())

let mutable output = []
let mutable index = [""]

let squareOf x =
    (x|> double) * (x|> double) |> double

let isSquare (x : double) =
    //(x * (10 |> double)) % (10|> double) = 0.0
    x - floor( x )|>double = 0.0


let perfectSquare x k = 
    let sumOfSquares = List.sumBy squareOf [x .. (x + k - 1)] |> double
    //printfn "%A" sumOfSquares
    let result = isSquare (sqrt( sumOfSquares |> double ))
    if result then
        //printfn "%d" x
        System.Console.WriteLine(x)
        output  <- [x] |> List.append output
        //printfn "%d" x

let traverseRange startIndex endIndex k =
    for i = startIndex to endIndex do
        perfectSquare i k

let printRange startIndex endIndex k =
    printfn "Reached"
    for i in startIndex .. endIndex do
        printfn "%d" i

let findPSquares = spawn system "findPSquares" <| fun mailbox ->
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        //| (a, b, c) -> printRange a b c
        | (a, b, c) -> traverseRange a b c
        return! loop()
    }
    loop()

let processRange (msg: string) = 
    let msg_split = msg.Split [|' '|] |> Array.toList
    //printfn "%d" (msg_split.Item(0)|>int)
    traverseRange (msg_split.Item(0)|>int) (msg_split.Item(1)|>int) (msg_split.Item(2)|>int)

type EchoServer(name) =
    inherit Actor()

    override x.OnReceive message =
        //let tid = Threading.Thread.CurrentThread.ManagedThreadId
        //printfn "%s %d" name tid
        match message with
        | :? string as msg -> processRange msg
        | _ ->  failwith "unknown message"

let echoServers = 
    [1 .. 10+1]
    |> List.map(fun id ->   let properties = [| string(id) :> obj |]
                            system.ActorOf(Props(typedefof<EchoServer>, properties)))




let N = 40
let k = 24
let m = 10 //number of actors 5000 - x4 or x5

//let seqRange = seq { 1 .. N/m .. N }
let seq1 = seq { for i in 1 .. m -> (i, ((i-1)*(N/m)+1)) }

for (id, i) in seq1 do
    //printfn "%d . %d %d %d" id i (i + N / m - 1) k
    id |> List.nth echoServers <! sprintf "%d %d %d" i (i + N / m - 1) k

//let outputList = String.concat "\n" output

printfn "%A" output

//printfn "%s" outputList

printfn "%A" index

//#time

(*
let system = ActorSystem.Create("FSharp")

type EchoServer(name) =
    inherit Actor()

    override x.OnReceive message =
        let tid = Threading.Thread.CurrentThread.ManagedThreadId
        match message with
        | :? string as msg -> printfn "Hello %s from %s at #%d thread" msg name tid
        | _ ->  failwith "unknown message"

let echoServers = 
    [1 .. 10]
    |> List.map(fun id ->   let properties = [| string(id) :> obj |]
                            system.ActorOf(Props(typedefof<EchoServer>, properties)))

let rand = Random(1234)

for id in [1 .. 10] do
    id |> List.nth echoServers <! sprintf "F# request %d!" id

system.Shutdown()
*)