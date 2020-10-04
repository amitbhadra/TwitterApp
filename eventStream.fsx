#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

open Akka.FSharp
open Akka.Configuration


// #Event Bus
// Originally conceived as a way to send messages to groups of actors, the EventBus has been generalized to Event Stream
// #Event Stream
// The event stream is the main event bus of each actor system: it is used for carrying log messages and Dead Letters and may be used by the user code for other purposes as well. It uses Subchannel Classification which enables registering to related sets of channels

let system = ActorSystem.Create("FSharp")

let echoServer = 
    spawn system "EchoServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                let tid = Threading.Thread.CurrentThread.ManagedThreadId
                printfn "Thread -- %d Message -- %s" tid message
                match box message with
                | :? string -> 
                    printfn "Echo '%s'" message
                    return! loop()
                | _ ->  failwith "unknown message"
            } 
        loop()

let eventStream = system.EventStream

eventStream.Subscribe(echoServer, typedefof<string>)

eventStream.Publish("Anybody home?")
eventStream.Publish("Knock knock")

system.Terminate()