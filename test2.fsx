
#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open System.Threading
open Akka.FSharp
open Akka.Configuration
open Akka.Routing
open System.Diagnostics
open System.Text.RegularExpressions


#time "on"

//----------------------------------------------------------------------------
// Authors - Amit Asish Bhadra and Rishabh Aryan Das
//----------------------------------------------------------------------------

let squareOf x =
    (x|> double) * (x|> double) |> double

let mutable ALL_COMPUTATIONS_DONE = 0

let mutable count = 0

let mutable res_str = ""

// Define the type of messages this program can send or receive
type MyMessage =
| MessageType1 of int*int
| MessageType2 of int
| MessageType3 of int

let mutable visited_actors = Array.empty

// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let mySlaveActor (mailbox: Actor<_>) =
    let rec loop n n_str = actor {
        //printfn "%s" (mailbox.Self.ToString())
        let! rcv = mailbox.Receive()
        let mutable value = -1
        match rcv with
        | MessageType2(N) -> 
            value <- N
            Array.set visited_actors N N
            return! loop N "abc"
        | MessageType3(N) ->
            printfn "%d" n
            printfn "%s" (mailbox.Self.ToString())
            return! loop n n_str
        
    }
    loop 0 ""

// This is the boss actor responsible for allotting parallel builds on to slaves
let myBossActor (mailbox: Actor<_>) =

    let rec loop() = actor {
        //printfn "%A" (mailbox.Self.GetHashCode)
        //printfn "%A" (mailbox.Self.GetType)

        let! rcv = mailbox.Receive()
        match rcv with
        | MessageType1(N,k) -> 
                        let num_of_actors = N
                        let workers =
                            [0 .. (num_of_actors - 1)]
                            |> List.map(fun id -> spawn mailbox (sprintf "workerActor%i" id) mySlaveActor)

                        let mutable j = 1
                        let mutable res_str = ""

                        visited_actors <- Array.zeroCreate N

                        for i in [0 .. num_of_actors-1] do
                            List.item (i) workers <! MessageType2(i)

                        for i in [0 .. num_of_actors-1] do
                            List.item (i) workers <! MessageType3(i)

                        printfn "%A" visited_actors

        | MessageType2(R) -> 
                        if R <> 0 then
                            printfn "%d" R
                        count <- count - 1
                        if count = 0 then
                            ALL_COMPUTATIONS_DONE <- 1

        return! loop()
    }
    loop()


    // main function used to take in parameters
let main argv =
    let N = ((Array.get argv 1) |> int)
    let k = ((Array.get argv 2) |> int)
    count <- N
    let system = System.create "system" <| ConfigurationFactory.Default()

    let bossActor = spawn system "boss_actor" myBossActor
    bossActor <! MessageType1(N, k)




    while(ALL_COMPUTATIONS_DONE = 0) do
        0|>ignore
        
    system.Terminate() |> ignore

    0
    
main fsi.CommandLineArgs
