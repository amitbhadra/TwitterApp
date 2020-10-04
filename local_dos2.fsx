//LocalActor.fsx
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote"


open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

type MyMessage =
| Type1 of int*int
| Type2 of int

let mutable FINISHED_FLAG = 0

let mutable count = 0

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

let bossActorFunction (mailbox: Actor<_>) =

    let rec loop() = actor {
        let! rcv = mailbox.Receive()
        match rcv with
        | Type1(N,k) -> printfn "%d %d" N k
                        let remotesystem = ActorSystem.Create("RemoteFSharp", configuration)
                        let echoClient = remotesystem.ActorSelection("akka.tcp://RemoteFSharp@192.168.0.137:9001/user/EchoServer")
                        //echoClient <? sprintf "%d %d" N k
                        //let task = echoClient <? Type1(N,k)
                        let task = echoClient <? sprintf "%d %d" N k
                        //echoClient <? sprintf "%d %d" N k
                        let response = Async.RunSynchronously (task, 100000000)
                        System.Console.WriteLine(string(response))
                        remotesystem.Terminate() |> ignore

        | Type2(R) -> if R <> 0 then
                        printfn "%d" R
                      count <- count - 1
                      if count = 0 then
                          FINISHED_FLAG <- 1
        //return! loop()
    }
    loop()

let main argv =
    let N = ((Array.get argv 1) |> int)
    let k = ((Array.get argv 2) |> int)

    count <- N
    let system = System.create "system" <| ConfigurationFactory.Default()

    let bossActor = spawn system "boss_actor" bossActorFunction
    bossActor <! Type1(N,k)
    
    while(FINISHED_FLAG = 0) do
        //Thread.Sleep(3000)
        0|>ignore

    //Console.ReadKey() |> ignore

    system.Terminate() |> ignore

    0
    

main fsi.CommandLineArgs