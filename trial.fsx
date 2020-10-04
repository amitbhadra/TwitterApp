(*
#I @".\packages\akka.fsharp\1.4.10\lib\netstandard2.0"
#I @".\packages\akka\1.4.10\lib\netstandard2.0"
#I @".\packages\newtonsoft.json\12.0.3\lib\net45"
#I @".\packages\fspickler\5.3.2\lib\net45"

#r "Akka.FSharp.dll"
#r "Akka.dll"
#r "Newtonsoft.Json.dll"
#r "FsPickler.dll"

*)

#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

open Akka.FSharp
open Akka.Configuration

let system = System.create "MySystem" <| ConfigurationFactory.Default()

type EchoServer(name) =
    inherit Actor()
    override x.OnReceive message =
        let tid = Threading.Thread.CurrentThread.ManagedThreadId
        printfn "%s %d %s" name tid (message|>string)
        match message with
        | :? string as msg -> printfn "%s %d %s" name tid msg
        | _ ->  failwith "unknown message"


let worker_ref =
    spawn system "worker_actor"
    <| fun mailbox ->
        let rec loop() = actor {
            let tid = Threading.Thread.CurrentThread.ManagedThreadId
            let! msg = mailbox.Receive()
            printfn "Thread -- %d Message -- %s" tid msg
            let sender = mailbox.Sender()
            sender <! msg
            // printf "\n%s\n" msg
            return! loop() }
        loop()

let boss_ref =
    spawn system "boss_actor"
    <| fun mailbox ->
        actor {
            let! msg = mailbox.Receive()
            let worker_ref = select "akka://MySystem/user/worker_actor" system

            //let res = system.ActorOf(Props(typedefof<EchoServer>, [| "1" :> obj |]))
            //res <! "aa"
            //let res = system.ActorOf(Props(typedefof<EchoServer>, [| "2" :> obj |]))
            //res <! "bb"
            // worker_ref <! msg
            //let reply = worker_ref <? msg
            let response = Async.RunSynchronously(worker_ref <? msg, 500)
            //let reply2 = worker_ref <? "Hi"
            let response2 = Async.RunSynchronously(worker_ref <? "Hi", 500)
            let response2 = Async.RunSynchronously(worker_ref <? "Hi2", 500)
            let response2 = Async.RunSynchronously(worker_ref <? "Hi3", 500)
            let response2 = Async.RunSynchronously(worker_ref <? "Hi4s", 500)
            printfn "\n%s" response
            printfn "\n%s" response2
            // let response = worker_ref <? msg
            // printf "%A" response
        }

boss_ref <! "Hello"

system.Terminate()