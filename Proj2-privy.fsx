//#load "bootstrap.fsx"
#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"
open System
open Akka.FSharp
open Akka.Configuration

type MyMessage =
| GossipValue of string
| PushSumValues of int*int
| Topology of int*int //topology code, number of nodes in topolgy

let system = System.create "system" <| ConfigurationFactory.Default()

let mutable ALL_COMPUTATIONS_DONE = 0

let mutable nodeActorRef = Array.empty

let getNeighbour(actor: string, topology: int, numNodes: int) =
    if topology = -1 then
        Console.WriteLine(sprintf "Unexpected topolgy in getNeighbour() for Actor %s" actor)
        exit(-1)
    if numNodes = -1 then
        Console.WriteLine(sprintf "Unexpected numNodes in getNeighbour() for Actor %s" actor)
        exit(-1)
    let currentActor = actor.Substring (actor.LastIndexOf '/' + 1) |> int
    let generator = Random()
    let mutable neighbour = generator.Next(numNodes)
    if topology = 1 then //Full Network
        while (neighbour = currentActor) do
            neighbour <- generator.Next(numNodes)
    neighbour


let nodeActorFunctionForGossip (mailbox: Actor<_>) =
    let mutable topology = -1
    let mutable numNodes = -1
    let mutable data = ""
    let rec loop rumorCounter = actor {
        let! msg = mailbox.Receive()
        // let system = System.create "system" <| ConfigurationFactory.Default()
        Console.WriteLine(sprintf "%i" rumorCounter)
        // printfn "bla"
        match msg with
        | Topology(t, n) ->
            topology <- t
            numNodes <- n
            printfn "bla"
            return! loop rumorCounter
        | GossipValue(data1) ->
            
            data <- data1
            if rumorCounter + 1 < 10 then
                let neighbour = 0
                //getNeighbour(mailbox.Self.ToString(), topology, numNodes)
                
                //let neighbourActor = system.ActorSelection(sprintf "akka://system/user/nodeActor%i" neighbour)
                nodeActorRef.[neighbour] <! data1
                return! loop (rumorCounter + 1)
            else
                ALL_COMPUTATIONS_DONE <- 1

        | _ ->
            Console.WriteLine("Gossip node received invalid data")
            exit(-1)
        Console.WriteLine(sprintf "Actor %s received message: %s" (mailbox.Self.ToString().Substring (mailbox.Self.ToString().LastIndexOf '/' + 1)) data)
        return! loop (rumorCounter + 1)
    }
    loop 0

let main =
    // let system = System.create "system" <| ConfigurationFactory.Default()
    let topology = Topology(1, 10)
    let message = GossipValue("Hello")
    nodeActorRef <- Array.zeroCreate (10)
    let system = System.create "system" <| ConfigurationFactory.Default()
    for i = 0 to 9 do
        let mutable worker_slave_actor = spawn system (sprintf "workerActor%i" i) nodeActorFunctionForGossip
        Array.set nodeActorRef i worker_slave_actor
        nodeActorRef.[i] <! Topology(1, 10)
    printfn "Reached1"
    nodeActorRef.[5] <! message
    printfn "Reached2"
    while(ALL_COMPUTATIONS_DONE = 0) do
        0|>ignore


main