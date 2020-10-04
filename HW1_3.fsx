
#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open System.Threading
open Akka.FSharp
open Akka.Configuration
open Akka.Routing
open System.Diagnostics

#time "on"


let encode N k =
    sprintf "%i %i" N k

let getN s =
    s.ToString().Substring(0, s.ToString().IndexOf(' '))

let getK s =
    s.ToString().Substring(s.ToString().IndexOf(' ')+1, s.ToString().Length - s.ToString().IndexOf(' ') - 1)

let squareOf x =
    (x|> double) * (x|> double) |> double

let mutable FINISHED_FLAG = 0

type returnMsg =
    | Calculate of string
    | Done of string

let slaveFunction (mailbox: Actor<'a>) =
    let rec loop() = actor {
        // let tid = Threading.Thread.CurrentThread.ManagedThreadId
        // printfn "TID %i" tid
        let! msg = mailbox.Receive()
        let message = msg.ToString()
        let vals : string[]=  message.Split[|' '|]
        let N = vals.[0] |> int
        let k = vals.[1] |>int
        let sender = mailbox.Sender()
        // printf "%i :: %s\n" N (mailbox.Self.ToString())
        //let mutable sum = 0
        //for i = N to (N+k)-1 do
        //    sum <- sum + i*i
        //if (round (sqrt (sum |> float))*round (sqrt (sum |> float))) = (sum |> float) then
            // System.Console.Write(sprintf "%i, " N)
        let sumOfSquares = (List.sumBy squareOf [ (N|>double) .. ((N|>double) + (k|>double) - (1|> double))] ) |> double
        let result = ((sqrt(sumOfSquares)|>double) - floor( sqrt(sumOfSquares)|>double ))|>double = (0.0|>double)
        if result then
            System.Console.WriteLine(N)
            sender <! N
        //else
            // System.Console.Write("0, ")
            //sender <! ""
        return! loop()
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
        let message = rcv.ToString()
        let vals : string[]=  message.Split[|' '|]
        let N = vals.[0] |> int
        let k = vals.[1] |>int

        let num_of_actors = 12
        //let mutable workerActorsRef = Array.zeroCreate N

        //let mutable ret_value = Array.zeroCreate N
        //let mutable res = Array.zeroCreate N

        // for i = 1 to N do
        //     Array.set workerActorsRef (i-1) (spawn mailbox (sprintf "worker_actor%i" (i-1)) slaveFunction)
        //     Array.set ret_value (i-1) ((Array.get workerActorsRef (i-1)) <? (encode i k))
        // return! loop()


        let mutable c = 0
        let workerStart =
            [0 .. (num_of_actors - 1)]
            |> List.map(fun id -> spawn mailbox (sprintf "workerActor%i" id) slaveFunction)

            (*
        for i = 1 to N do
            if i%(num_of_actors) = 0 then
                c <- 0
            Array.set ret_value (i-1) ((List.item (c) workerStart) <? (encode i k))
            c <- c+1
            *)

        let mutable j = 1
        let mutable res_str = ""

        while (j <= N) do
            for i in [0 .. num_of_actors-1] do
                if j+i <= N then
                    //printf "%A" << 
                    List.item (i) workerStart <! sprintf "%d %d" (j+i) k
                    //de (j+i) k) |> ignore
                    //Array.set ret_value (j+i-1) ((List.item (i) workerStart) <? sprintf "%d %d" (j+i) k)
                    //res_str <- res_str + ((List.item (i) workerStart) <? (encode (j+i) k))|>string
                    //i |> List.nth echoServers <! sprintf "%d %d" (j+i) k
            j <- j + num_of_actors

            
        //for i = 0 to N-1 do
            //Array.set res (i) (Async.RunSynchronously(Array.get ret_value (i), 1000))
            //Array.set res (i) (Async.RunSynchronously(Array.get ret_value (i), num_of_actors))
            //if (Array.get res i) <> 0 then
              //  printfn "%d" (Array.get res i)
            

        FINISHED_FLAG <- 1
        //Console.ReadKey() |> ignore

        //let sender = mailbox.Sender()
        //sender <! sprintf "Done Parent"
        //return! loop()
        // printf "Final result: "
        // for i = 0 to N-1 do
        //     if (Array.get res i) <> 0 then
        //         printfn "%d" (Array.get res i)
    }
    loop()

type returnMsg =
    | Done of string

let main argv =
    let N = ((Array.get argv 1) |> int)
    let k = ((Array.get argv 2) |> int)
    let system = System.create "system" <| ConfigurationFactory.Default()

    let bossActor = spawn system "boss_actor" bossActorFunction
    bossActor <! sprintf "%d %d" N k
    //let echoClient = system.ActorSelection("akka.tcp://RemoteFSharp@localhost:9001/user/EchoServer")

    //let task = echoClient <? "F#!"

    //let response = Async.RunSynchronously (task, 1000)

    //let response = Async.RunSynchronously (task, 1000)
    //let response = Async.RunSynchronously (res, 10000)

    while(FINISHED_FLAG = 0) do
        //Thread.Sleep(3000)
        0|>ignore

//    Console.ReadKey() |> ignore

    system.Terminate() |> ignore

    //System.Console.ReadLine() |> ignore
    0


main fsi.CommandLineArgs
