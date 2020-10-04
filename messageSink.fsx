#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

let system = System.create "system" (Configuration.defaultConfig())

let print msg =
  printfn "Message received: %A" msg

let actorOfSink (f : 'a -> unit) = actorOf f

let printActorRef =
  actorOfSink print
  |> spawn system "print-actor"

printActorRef <! 3
// "Message received: 3" is printed