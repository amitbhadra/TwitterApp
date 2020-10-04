#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

open Akka.FSharp
open Akka.Configuration

let system = ActorSystem.Create("FSharp")

let printMessage msg =
  printfn "%s" msg

let routeSensorData msg =
  msg |> Seq.map (fun x -> ("sensor-actor-", msg))


let actorOfRouteToChildren f spawnChild (mailbox : Actor<'a>) =

  let getActor id =
    let actorRef = mailbox.Context.Child(id)
    if actorRef.IsNobody() then
      spawnChild id mailbox
    else
      actorRef

  let rec imp () =
    actor {
      let! msg = mailbox.Receive()
      //f msg (getActor id)
      f msg |> Seq.iter (fun (id, x) -> (getActor id) <! x)
      return! imp ()
    }

  imp ()


let boss_ref =
    spawn system "boss_actor"
    <| fun mailbox ->
        actor {
            let! msg = mailbox.Receive()
            //let worker_ref = select "akka://MySystem/user/worker_actor" system
            //let response = Async.RunSynchronously(worker_ref <? msg, 500)

            let sensorRouterRef =
              actorOfRouteToChildren routeSensorData (spawnChild square)
              |> spawn system "route-sensor-actor"

            printfn "\n%s" msg
        }


  boss_ref <! "Hello"

  system.Terminate()