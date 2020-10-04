//LocalActor.fsx
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote"
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                deployment {
                    /remoteecho {
                        remote = ""akka.tcp://RemoteFSharp@192.168.0.137:9001""
                    }
                }
            }
            remote {
                helios.tcp {
                    port = 0
                    hostname = ""127.0.0.1""
                }
            }
        }")
let system = ActorSystem.Create("RemoteFSharp", configuration)
let echoClient = system.ActorSelection("akka.tcp://RemoteFSharp@192.168.0.137:9001/user/EchoServer")
let task = echoClient <? "F#!"
let response = Async.RunSynchronously (task, 10000)
printfn "Reply from remote %s" (string(response))