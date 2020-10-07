﻿
#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.Remote.dll"

open System
open System.Threading
open Akka.FSharp
open Akka.Configuration
open Akka.Routing
open System.Diagnostics
open System.Text.RegularExpressions


#time "on"

//----------------------------------------------------------------------------
// Authors - Amit Asish Bhadra and Rishabh Aryan Das
//----------------------------------------------------------------------------

let mutable ALL_COMPUTATIONS_DONE = 0

let timer = System.Diagnostics.Stopwatch()

// Define the type of messages this program can send or receive
type MyMessage =
| MessageType1 of int*int
| MessageType2 of int
| MessageType3 of int*string
| MessageType4 of Array
| MessageType5 of int
| MessageType6 of int*string*string*Akka.Actor.ActorSystem
| MessageType7 of string
| MessageType8 of int
| MessageType9 of int*int*string*string
| MessageType10 of int*int*string
| MessageType11 of int
| MessageType12 of string
| MessageType13 of int
| MessageType14 of int

let find_left_neighbor n max_index = 
    if (n<>1) then
        n-1
    else 
        -1

let find_right_neighbor n max_index  = 
    if (n<>max_index) then
        (n+1)
    else
        -1

let find_left_neighbor_2d n max_index = 
    if (n-1)%max_index <> 0 then
        n-1
    else 
        -1

let find_right_neighbor_2d n max_index  = 
    if (n+1)%max_index <> 1 then
        (n+1)
    else
        -1

let find_up_neighbor_2d n max_index = 
    if (n-max_index) > 0 then
        (n-max_index)
    else
        -1

let find_down_neighbor_2d n max_index = 
    if (n+max_index) <= max_index*max_index then
        (n+max_index)
    else
        -1


let mutable slave_actor_refs = Array.empty
let mutable converged_actors = Array.empty
let mutable visited_actors = Array.empty
let mutable boss_actor_ref = Array.empty
let mutable alive_nodes = Array.empty
let mutable converged_nodes = 0
let mutable topo = ""
let mutable num_nodes = 0
let mutable max_index_2d = 0


let converged_nodes_array idx rumor= 
    if(converged_actors.[idx] = 0) then
        //printfn "Converged %d" idx
        converged_actors.[idx] <- 1
        alive_nodes <- alive_nodes |> Array.filter ((<>)idx)
        converged_nodes <- converged_nodes + 1
        //printfn "Total finished %d" converged_nodes
    if(alive_nodes.Length = 1 || converged_nodes = num_nodes) then
        printfn "------------------------ALL DONE------------------------"
        timer.Stop()
        printfn "%f" timer.Elapsed.TotalMilliseconds                                   
        ALL_COMPUTATIONS_DONE <- 1
        exit(0)
    let mutable random = new System.Random()
    let mutable new_random_idx = random.Next(1, alive_nodes.Length)
    //Console.WriteLine("Sending message to {0}", [alive_nodes.[new_random_idx]])
    slave_actor_refs.[alive_nodes.[new_random_idx]] <! MessageType12(rumor)



// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let mySlaveActor (mailbox: Actor<_>) =
    let rec loop actor_idx rumor visited neighbors = actor {

        let! rcv = mailbox.Receive()

        match rcv with
        | MessageType2(idx) ->
                        if (topo = "line") then
                            //let mutable n_left = find_left_neighbor idx num_nodes
                            //let mutable n_right = 
                            let mutable return_array = Array.create 2 -1
                            return_array.[0] <- find_left_neighbor idx num_nodes
                            return_array.[1] <- find_right_neighbor idx num_nodes
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            //printfn "For %d return array is %A" idx return_array
                            return! loop idx rumor visited return_array

                        if (topo = "full") then
                            let mutable return_array = Array.create (num_nodes+1) -1
                            for i in 1..num_nodes do
                                return_array.[i] <- i
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            return! loop idx rumor visited return_array

                        if (topo = "2D" || topo = "imp2D") then
                            let mutable return_array = Array.create 4 -1
                            return_array.[0] <- find_down_neighbor_2d idx max_index_2d
                            return_array.[1] <- find_up_neighbor_2d idx max_index_2d
                            return_array.[2] <- find_right_neighbor_2d idx max_index_2d
                            return_array.[3] <- find_left_neighbor_2d idx max_index_2d
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            //printfn "For %d return array is %A" idx return_array
                            return! loop idx rumor visited return_array
                        
        | MessageType12(rumor_string) ->

                        if (visited_actors.[actor_idx] < 10) then
                            slave_actor_refs.[actor_idx] <! MessageType7(rumor_string)
                            return! loop actor_idx rumor_string (visited+1) neighbors
                        else
                            converged_nodes_array actor_idx rumor
                            return! loop actor_idx rumor_string visited neighbors

        | MessageType7(rumor_string) ->
                        visited_actors.[actor_idx] <- (visited_actors.[actor_idx] + 1)

                        if(visited_actors.[actor_idx] >= 10) then
                            converged_nodes_array actor_idx rumor
                            return! loop actor_idx rumor_string (visited+1) neighbors
                        else
                            let mutable random = new System.Random()

                            let mutable advanced_neighbors = neighbors
                            
                            if(topo = "imp2D") then
                                let mutable all_actors = [|1..num_nodes|]
                                for i in 0..advanced_neighbors.Length-1 do
                                    all_actors <- all_actors |> Array.filter ((<>) advanced_neighbors.[i] )
                                let mutable random_all_actors_idx = random.Next(0, all_actors.Length)
                                advanced_neighbors <- Array.append advanced_neighbors [|all_actors.[random_all_actors_idx]|]
                                
                            let mutable random_idx = random.Next(0, advanced_neighbors.Length)

                            // iterate neighbors.Length * 2 times over the neighbors and if all are visited then choose a random number
                            // neighbors.Length * 2 is chosen because we don't want the loop to be ever-lasting and it seems like a 
                            // reasonable enough value
                            let mutable iterator = 0

                            while (visited_actors.[advanced_neighbors.[random_idx]] < 10 && iterator <= advanced_neighbors.Length * 2) do
                                random_idx <- random.Next(0, advanced_neighbors.Length)
                                iterator <- iterator + 1

                            if(visited_actors.[advanced_neighbors.[random_idx]] < 10) then
                                slave_actor_refs.[advanced_neighbors.[random_idx]] <! MessageType12(rumor)
                            else
                                let mutable random = new System.Random()
                                let mutable new_random_idx = random.Next(1, alive_nodes.Length)
                                slave_actor_refs.[new_random_idx] <! MessageType12(rumor)

                            return! loop actor_idx rumor_string (visited+1) neighbors
        | _ -> printfn "Incorrect entry"                
    }
    loop 0 "abc" 0 [||]

// This is the boss actor responsible for allotting parallel builds on to slaves
let myBossActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! rcv = mailbox.Receive()
        match rcv with
        | MessageType6(max_nodes, topology, algorithm, system) ->

                        topo <- topology
                        num_nodes <- max_nodes
                        if (topo = "2D" || topo ="imp2D") then
                            max_index_2d <- int(ceil(sqrt(float(num_nodes))))
                            num_nodes <- max_index_2d*max_index_2d
                        

                        slave_actor_refs <- Array.zeroCreate (num_nodes+1)
                        converged_actors <- Array.zeroCreate (num_nodes+1)
                        visited_actors <- Array.zeroCreate (num_nodes+1)
                        alive_nodes <- Array.zeroCreate (num_nodes+1)

                        // Initialize the slave actors and then store them in a refs array

                        for i in 1..num_nodes do
                            let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) mySlaveActor
                            Array.set slave_actor_refs i worker_slave_actor
                            slave_actor_refs.[i] <! MessageType2(i)
                            alive_nodes.[i] <- i

                        printfn "Done with slave actors initialization!"
                        
                        timer.Start()

                        if (algorithm = "gossip") then
                            slave_actor_refs.[1] <! MessageType12("Hello")
        | _ -> printfn "Incorrect entry"                    
        return! loop()
    }
    loop()


// main function used to take in parameters
let main argv =
    let num_nodes = ((Array.get argv 1) |> int)
    let topology = ((Array.get argv 2) |> string)
    let algorithm = ((Array.get argv 3) |> string)
    

    let system = System.create "system" <| ConfigurationFactory.Default()

    let bossActor = spawn system "boss_actor" myBossActor

    boss_actor_ref <- Array.zeroCreate 1
    boss_actor_ref.[0] <- bossActor

    bossActor <! MessageType6(num_nodes, topology, algorithm, system)


    while(ALL_COMPUTATIONS_DONE = 0) do
        0|>ignore

    system.Terminate() |> ignore
    0

main fsi.CommandLineArgs
