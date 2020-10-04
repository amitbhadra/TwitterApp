#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

let system = System.create "system" (Configuration.defaultConfig())

let square msg =
  msg * msg

let actorOfConvert f outputRef =
  actorOf2 (fun _ msg -> outputRef <! f msg)

let print msg =
  printfn "Message received: %A" msg

let actorOfSink (f : 'a -> unit) = actorOf f

let printActorRef =
  actorOfSink print
  |> spawn system "print-actor"

let squareActorRef =
  actorOfConvert square printActorRef
  |> spawn system "square-actor"

squareActorRef <! 3
// "Message received: 9" is printed