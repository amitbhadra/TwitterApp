
#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open Akka.FSharp
open Akka.Configuration
open Akka.Routing
open System.Diagnostics



#time "on"

let encode N k =
    printfn "wow"
    sprintf "%i %i" N k

let getN s =
    s.ToString().Substring(0, s.ToString().IndexOf(' '))

let getK s =
    s.ToString().Substring(s.ToString().IndexOf(' ')+1, s.ToString().Length - s.ToString().IndexOf(' ') - 1)

let squareOf x =
    (x|> double) * (x|> double) |> double

let slaveFunction (mailbox: Actor<'a>) =
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        printfn "gg"
        let tid = Threading.Thread.CurrentThread.ManagedThreadId
        printfn "TID %i" tid
        let N = getN msg |> int
        let k = getK msg |> int
        let sender = mailbox.Sender()
        printf "%i :: %s\n" N (mailbox.Self.ToString())
        let mutable sum = 0
        let sumOfSquares = (List.sumBy squareOf [ (N|>int) .. ((N|>int) + (k|>int) - 1)]) |> double
        let result = (sqrt(sumOfSquares) - floor( sqrt(sumOfSquares) ))|>double = 0.0
        if result then
            System.Console.WriteLine(N)
        return! loop ()
    }
    loop()

(*
let main argv =
    let N = ((Array.get argv 1) |> int)
    let k = ((Array.get argv 2) |> int)
    let system = System.create "system" <| ConfigurationFactory.Default()
    let bossActor =
        spawn system "BossActor"
        <| fun mailbox ->
            actor {
                let slaveActors = spawnOpt mailbox "slaveActors" slaveFunction [SpawnOption.Router (RoundRobinPool(500))]
                let! msg = mailbox.Receive()
                let mutable ret_value = Array.zeroCreate N
                let mutable res = Array.zeroCreate N
                for i = 1 to N do
                    Array.set ret_value (i-1) (slaveActors <? (encode i k))
                // for i = 0 to N-1 do
                //     Array.set res (i) (Async.RunSynchronously(Array.get ret_value (i), 1000))
                //     if (Array.get res i) <> 0 then
                //         printfn "%d" (Array.get res i)
            }
    // time (fun() -> bossActor <! "Start")
    system.Terminate() |> ignore
    0

    *)

// let main2 argv =
//     let N = ((Array.get argv 1) |> int)
//     let k = ((Array.get argv 2) |> int)
//     let system = System.create "system" <| ConfigurationFactory.Default()
//     let slaveActors = spawnOpt system "slaveActors" (actorOf2 slaveFunction) [SpawnOption.Router (RoundRobinPool(N))]
//     let mutable ret_value = Array.zeroCreate N
//     let mutable res = Array.zeroCreate N
//     for i = 1 to N do
//         Array.set ret_value (i-1) (slaveActors <? (encode i k))
//     for i = 0 to N-1 do
//         Array.set res (i) (Async.RunSynchronously(Array.get ret_value (i), 1000))
//         if (Array.get res i) <> 0 then
//             printfn "%d" (Array.get res i)


let bossActorFunction (mailbox: Actor<'a>) =
    let rec loop() = actor {
        let! rcv = mailbox.Receive()
        let msg = (box rcv).ToString()
        let N = (getN msg |> int)
        let k = (getK msg |> int)

        let num_of_actors = 100 //number of actors 5000 - x4 or x5
        let mutable j = 1
        printfn "reached"
        // let mutable workerActorsRef = Array.zeroCreate N

        // let mutable ret_value = Array.zeroCreate N

        // for i = 1 to N do
        //     Array.set workerActorsRef (i-1) (spawn mailbox (sprintf "worker_actor%i" (i-1)) slaveFunction)
        //     Array.set ret_value (i-1) ((Array.get workerActorsRef (i-1)) <? (encode i k))
        // return! loop()

        let workerStart =
            [0 .. num_of_actors]
            |> List.map(fun id -> spawn mailbox (sprintf "workerActor%i" id) slaveFunction)

        printfn "reached2"

        while (j <= N) do
            printfn "%d" (j)
            for i in [0 .. num_of_actors-1] do
                if j+i <= N then
                    printfn "R--%d" (j+i)
                    //List.item (i-1) workerStart <! (encode (j+i) k)
                    (i-1) |> List.nth workerStart <! (encode (j+i) k)
            j <- j + num_of_actors

        printfn "reached"
        //for i = 1 to N do
            //List.item (i-1) workerStart <! (encode i k)
    }
    loop()



let main argv =
    let N = ((Array.get argv 1) |> int)
    let k = ((Array.get argv 2) |> int)
    let system = System.create "system" <| ConfigurationFactory.Default()
    let bossActor = spawn system "boss_actor" bossActorFunction
    bossActor <! (encode N k)
    system.Terminate() |> ignore
    0


main fsi.CommandLineArgs