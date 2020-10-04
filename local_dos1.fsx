//LocalActor.fsx
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote"


//----------------------------------------------------------------------------
// Authors - Amit Asish Bhadra and Rishabh Aryan Das
//----------------------------------------------------------------------------


open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

//This is the configuration for my local machine
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                deployment {
                    /remoteecho {
                        remote = ""akka.tcp://RemoteFSharp@localhost:9001""
                    }
                }
            }
            remote {
                helios.tcp {
                    port = 0
                    hostname = ""192.168.0.79""
                }
            }
        }")

// Man function used to call the 2 computers/nodes to check for perfect squares
let main argv =
    let N = ((Array.get argv 1) |> int)
    let k = ((Array.get argv 2) |> int)
    let number_of_computers = 2
    let system = ActorSystem.Create("RemoteFSharp", configuration)
    let echoClient = system.ActorSelection("akka.tcp://RemoteFSharp@192.168.0.137:9001/user/EchoServer")
    let echoClient2 = system.ActorSelection("akka.tcp://RemoteFSharp@localhost:9002/user/EchoServer")

    let task = echoClient <? sprintf "%d %d %d" 1 (N/number_of_computers) k
    let response = Async.RunSynchronously (task, 100000000)
    let task2 = echoClient2 <? sprintf "%d %d %d" (N/number_of_computers + 1) N k
    let response2 = Async.RunSynchronously (task2, 100000000)
    System.Console.WriteLine(string(response))
    System.Console.WriteLine(string(response2))
    
    system.Terminate() |> ignore

    0
    

main fsi.CommandLineArgs