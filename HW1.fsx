//#I @".\packages\akka.fsharp\1.4.10\lib\netstandard2.0"
//#I @".\packages\akka\1.4.10\lib\netstandard2.0"
//#I @".\packages\newtonsoft.json\12.0.3\lib\net45"
//#I @".\packages\fspickler\5.3.2\lib\net45"

//#r "Akka.FSharp.dll"
//#r "Akka.dll"
//#r "nuget:Newtonsoft.Json.dll"
//#r "FsPickler.dll"

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
    sprintf "%i %i" N k

let getN s =
    s.ToString().Substring(0, s.ToString().IndexOf(' '))

let getK s =
    s.ToString().Substring(s.ToString().IndexOf(' ')+1, s.ToString().Length - s.ToString().IndexOf(' ') - 1)

let squareOf x =
    (x|> double) * (x|> double) |> double

let slaveFunction (mailbox: Actor<'a>) msg =
    let N = getN msg |> int
    let k = getK msg |> int
    let sender = mailbox.Sender()
    //let mutable sum = 0
    //for i = N to (N+k)-1 do
    //    sum <- sum + i*i
    //if (round (sqrt (sum |> float))*round (sqrt (sum |> float))) = (sum |> float) then
    //    sender <! N
    //else
    //    sender <! 0
    let sumOfSquares = (List.sumBy squareOf [ (N|>int) .. ((N|>int) + (k|>int) - 1)]) |> double
    let result = (sqrt(sumOfSquares) - floor( sqrt(sumOfSquares) ))|>double = 0.0
    if result then
        sender <! N
    else
        sender <! 0

// let time f = 
//     let proc = Process.GetCurrentProcess()
//     let cpu_time_stamp = proc.TotalProcessorTime
//     let timer = new Stopwatch()
//     timer.Start()
//     try
//         f()
//     finally
//         let cpu_time = (proc.TotalProcessorTime-cpu_time_stamp).TotalMilliseconds
//         printfn "CPU time = %dms" (int64 cpu_time)
//         printfn "Absolute time = %dms" timer.ElapsedMilliseconds

let main argv =
    let N = ((Array.get argv 1) |> int)
    let k = ((Array.get argv 2) |> int)
    let system = System.create "system" <| ConfigurationFactory.Default()
    let bossActor =
        spawn system "BossActor"
        <| fun mailbox ->
            actor {
                let num_actors = 1000
                let slaveActors = spawnOpt mailbox "slaveActors" (actorOf2 slaveFunction) [SpawnOption.Router (RoundRobinPool(num_actors))]
                let! msg = mailbox.Receive()
                let mutable ret_value = Array.zeroCreate N
                let mutable res = Array.zeroCreate N
                for i = 1 to N do
                    Array.set ret_value (i-1) (slaveActors <? (encode i k))
                for i = 0 to N-1 do
                    Array.set res (i) (Async.RunSynchronously(Array.get ret_value (i), 1000))
                    if (Array.get res i) <> 0 then
                        printfn "%d" (Array.get res i)
            }
    bossActor <! "Start"
    system.Terminate() |> ignore
    0

main fsi.CommandLineArgs