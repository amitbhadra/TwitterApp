// Learn more about F# at http://fsharp.org

#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

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


let squareOf x =
    (x|> double) * (x|> double) |> double

let myActor (mailbox: Actor<_>)= 
    let rec loop () = actor {
        let! message  =  mailbox.Receive () 
        match box message with 
        | :? string as message ->   //let msg_split = message.Split [|' '|] |> Array.toList
                                    //let tid = Threading.Thread.CurrentThread.ManagedThreadId
                                    //System.Console.WriteLine(tid)
                                    let vals : string[]=  message.Split[|' '|]
                                    let index = vals.[0]
                                    let k = vals.[1]
                                    let sumOfSquares = (List.sumBy squareOf [ (index|>double) .. ((index|>double) + (k|>double) - (1|> double))] ) |> double
                                    let result = ((sqrt(sumOfSquares)|>double) - floor( sqrt(sumOfSquares)|>double ))|>double = (0.0|>double)
                                    if result then
                                        System.Console.WriteLine(index)
                                    return! loop ()
        | _ ->  failwith "unknown message"
  
    }
    loop ()

let x id = "myActor" + string(id)
let echoServers = 
    [1 .. 12+1]
    |> List.map(fun id -> let nex_x = x id
                          spawn system nex_x myActor)



//let seqRange = seq { 1 .. N/m .. N }
//let seq1 = seq { for i in 1 .. m -> (i, ((i-1)*(N/m)+1)) }

//for (id, i) in seq1 do
    //printfn "%d %d %d" i (i + N / m - 1) k
    //id |> List.nth echoServers <! sprintf "%d %d %d" i (i + N / m - 1) k




//let outputList = String.concat "\n" output

//printfn "%s" outputList

let N = 1000000
let k = 24
let num_of_actors = 12 //number of actors 5000 - x4 or x5
let mutable j = 1

while (j <= N) do
    for i in [0 .. num_of_actors-1] do
        if j+i <= N then
            i |> List.nth echoServers <! sprintf "%d %d" (j+i) k
    j <- j + num_of_actors