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

let system = System.create "system" (Configuration.defaultConfig())

let squareOf x =
    (x|> double) * (x|> double) |> double   

type EchoServer(name) = 
    inherit Actor()

    override x.OnReceive message = 
        match message with
        | :? string as msg ->   let msg_split = msg.Split [|' '|] |> Array.toList
                                let k = (msg_split.Item(2)|>int)
                                for i = (msg_split.Item(0)|>int) to (msg_split.Item(1)|>int) do
                                    let sumOfSquares = List.sumBy squareOf [i .. (i + k - 1)] |> double
                                    let result = sqrt(sumOfSquares) - floor( sqrt(sumOfSquares) )|>double = 0.0
                                    if result then
                                        System.Console.WriteLine(i)
        | _ ->  failwith "unknown message"
    

let echoServers = 
    [1 .. 1000+1]
    |> List.map(fun id ->   let properties = [| string(id) :> obj |]
                            system.ActorOf(Props(typedefof<EchoServer>, properties)))




let N = 1000000
let k = 24
let m = 1000 //number of actors 5000 - x4 or x5

//let seqRange = seq { 1 .. N/m .. N }
let seq1 = seq { for i in 1 .. m -> (i, ((i-1)*(N/m)+1)) }

for (id, i) in seq1 do
    //printfn "%d . %d %d %d" id i (i + N / m - 1) k
    id |> List.nth echoServers <! sprintf "%d %d %d" i (i + N / m - 1) k
    