#time "on"

#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open Akka.FSharp
open Akka.Configuration
open Akka.Routing
open System.Diagnostics
open Akka.Actor


// #Remote Actor
// Actor is not only a concurrency model, it can also be used for distributed computing.
// This example builds an EchoServer using an Actor.
// Then it creates a client to access the Akka URL.
// The usage is the same as with a normal Actor.

let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    port = 9001
                    hostname = localhost
                }
            }
        }")

let system = ActorSystem.Create("RemoteFSharp", configuration)

let echoServer = 
    spawn system "EchoServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                let sender = mailbox.Sender()
                match box message with
                | :? string -> 
                        printfn "super!"
                        sender <! sprintf "Hello %s remote" message
                        return! loop()
                | _ ->  failwith "unknown message"
            } 
        loop()

let echoClient = system.ActorSelection(
                            "akka.tcp://RemoteFSharp@localhost:9001/user/EchoServer")

let task = echoClient <? "F#!"

//let response = Async.RunSynchronously (task, 1000)

let response = Async.RunSynchronously (task, 1000)

printfn "Reply from remote %s" (string(response))

system.Terminate()