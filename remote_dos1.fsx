// RemoteActor.fsx
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

#time "on"

//----------------------------------------------------------------------------
// Authors - Amit Asish Bhadra and Rishabh Aryan Das
//----------------------------------------------------------------------------

// This computes th square of a number
let squareOf x =
    (x|> double) * (x|> double) |> double

let mutable ALL_COMPUTATIONS_DONE = 0
let mutable FINAL_RES = ""

// Slave actor definition
let mySlaveActor (mailbox: Actor<'a>) =
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        let message = msg.ToString()
        let vals : string[]=  message.Split[|' '|]
        let N = vals.[0] |> int
        let k = vals.[1] |>int
        let sender = mailbox.Sender()
        let sumOfSquares = (List.sumBy squareOf [ (N|>double) .. ((N|>double) + (k|>double) - (1|> double))] ) |> double
        let result = ((sqrt(sumOfSquares)|>double) - floor( sqrt(sumOfSquares)|>double ))|>double = (0.0|>double)
        if result then
            sender <! N
        else
            sender <! 0
        return! loop()
    }
    loop()

// Boss actor definition
let myBossActor (mailbox: Actor<'a>) =

    let rec loop() = actor {
        let! rcv = mailbox.Receive()
        let message = rcv.ToString()
        let vals : string[]=  message.Split[|' '|]
        let start_index = vals.[0] |>int
        let end_index = vals.[1] |> int
        let k = vals.[2] |>int

        let num_of_actors = 8

        let mutable ret_value = Array.zeroCreate end_index
        let mutable res = Array.zeroCreate end_index

        let workers =
            [0 .. (num_of_actors - 1)]
            |> List.map(fun id -> spawn mailbox (sprintf "workerActor%i" id) mySlaveActor)

        let mutable j = start_index
        let mutable res_str = ""

        while (j <= end_index) do
            for i in [0 .. num_of_actors-1] do
                if j+i <= end_index then
                    Array.set ret_value (j+i-1) ((List.item (i) workers) <? sprintf "%d %d" (j+i) k)
                    //Array.set ret_value (j+i-1) ((List.item (i) workers) <? sprintf "%d %d" (j+i) k)
            j <- j + num_of_actors

        for i = start_index to end_index-1 do
            Array.set res (i) (Async.RunSynchronously(Array.get ret_value (i), 1000))
            if (Array.get res i) <> 0 then
                res_str <- res_str + string((Array.get res i)) + "\n"
                
        FINAL_RES <- res_str

        ALL_COMPUTATIONS_DONE <- 1

        let sender = mailbox.Sender()
        sender <! res_str
        return! loop()
    }
    loop()

// Configuration for remote on localhost
let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = ""localhost""
                port = 9002
            }
        }"

// This is the representation of the main program
let system = System.create "RemoteFSharp" config
let echoServer = 
    spawn system "EchoServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                let sender = mailbox.Sender()

                match box message with
                | :? string -> 
                    let msg = message.ToString()
                    let vals : string[]=  msg.Split[|' '|]
                    let start_index = vals.[0] |>int
                    let end_index = vals.[1] |> int
                    let k = vals.[2] |>int


                    let bosssystem = System.create "system" <| ConfigurationFactory.Default()

                    let bossActor = spawn bosssystem "boss_actor" myBossActor

                    bossActor <! sprintf "%d %d %d" start_index end_index k

                    while(ALL_COMPUTATIONS_DONE = 0) do
                        0|>ignore
                    bosssystem.Terminate() |> ignore

                    sender <! FINAL_RES
                | _ ->  failwith "Unknown message"
                
            }
        loop()
Console.ReadLine() |> ignore
