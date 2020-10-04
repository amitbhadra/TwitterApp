
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

let num_of_actors = 10

// Define the type of messages this program can send or receive
type MyMessage =
| MessageType1 of int*int
| MessageType2 of int
| MessageType3 of int*string
| MessageType4 of Array
| MessageType5 of int
| MessageType6 of int*string*string
| MessageType7 of string
| MessageType8 of int
| MessageType9 of int*int*string*string
| MessageType10 of int*int*string
| MessageType11 of int

let find_left_neighbor n max_index = 
    if (n<>1) then
        (n-1)
    else
        -1

let find_right_neighbor n max_index  = 
    if (n<>max_index) then
        (n+1)
    else
        -1

let find_up_neighbor n max_index = 
    if (n<>1) then
        (n-1)
    else
        -1

let find_down_neighbor n max_index = 
    if (n<>max_index) then
        (n+1)
    else
        -1

let mutable slave_actor_refs = Array.empty
let mutable neighbor_actor_ref = Array.empty
let mutable visited_actors = Array.empty
let mutable converged_nodes = 0
let mutable topo = ""
let mutable num_nodes = 0
        
let find_my_neighbors (mailbox: Actor<_>) = 
    let rec loop() = actor {

        let! rcv = mailbox.Receive()
        match rcv with
        | MessageType3(nodes, topology) ->
                            topo <- topology
                            num_nodes <- nodes
                            visited_actors <- Array.zeroCreate (nodes+1)
                            printfn "%s" topology
                            return! loop() //nodes converged_nodes topology
        | MessageType2(n) ->
                            //printfn "%s %d %d" topo num_nodes n
                            //Console.WriteLine("FInding neighbors for = {0}", n)
                            if (topo = "line") then
                                let mutable n_left = find_left_neighbor n num_nodes
                                let mutable n_right = find_right_neighbor n num_nodes
                                let mutable return_array = Array.create 2 -1
                                let mutable array_index = 0

                                //Console.WriteLine("Left found - {0}", n_left)
                                //Console.WriteLine("Right found - {0}", n_right)
                                if (n_left > 0 && visited_actors.[n_left] <> 10) then
                                    Array.set return_array array_index n_left
                                    array_index <- (array_index + 1)
                                if (n_right > 0 && visited_actors.[n_right] <> 10) then
                                    Array.set return_array array_index n_right

                                //Console.WriteLine("Neighbors Array : ")
                                //Console.WriteLine("[{0}]", String.Join(", ", return_array))
                                let random = new System.Random()

                                let mutable random_idx = random.Next(0, array_index+1)
                                let sender = mailbox.Sender()
                                //if(return_array.[random_idx] <> 0) then
                                if(return_array.[random_idx] > 0) then
                                    //Console.WriteLine( "Random idx = {0} and element at idx = {1} ", random_idx,return_array.[random_idx])
                                    sender <! MessageType8(return_array.[random_idx])
                                else
                                    // choose the first neighbor to transmit who has not converged
                                    //Console.WriteLine("Choosing random neighbor ")
                                    let mutable first_index = -1
                                    let mutable iterator = 1
                                    while(first_index = -1 && iterator <= num_nodes) do
                                        if (visited_actors.[iterator] <= 10) then
                                            first_index <- iterator
                                        iterator <- iterator + 1
                                    //Console.WriteLine("First index found = {0}", first_index)
                                    if (first_index = -1) then
                                        neighbor_actor_ref.[0] <! MessageType11(-1)
                                    else
                                        sender <! MessageType8(first_index)
                                //return! loop num_nodes converged_nodes topo
                                //sender <! MessageType4(return_array)
        | MessageType5(actor_idx) ->
                                Array.set visited_actors actor_idx (Array.get visited_actors actor_idx + 1)
                                if (Array.get visited_actors actor_idx > 10) then
                                    //if (float(converged_nodes) >= (float(num_nodes) * 0.9)) then
                                    //    printfn "Converged 9 out of every 10 nodes!"
                                    //    ALL_COMPUTATIONS_DONE <- 1
                                    converged_nodes <- converged_nodes+1
                                    //printfn "Converged nodes = %d and num_nodes = %f" converged_nodes (float(num_nodes) * 0.9)
                                    return! loop()// num_nodes (converged_nodes+1) topo
        | MessageType8(actor_idx) ->
                                Array.set visited_actors actor_idx 11
                                //if (float(converged_nodes + 1) >= (float(num_nodes) * 0.9)) then
                                //    printfn "Converged 9 out of every 10 nodes!"
                                //    ALL_COMPUTATIONS_DONE <- 1
                                converged_nodes <- converged_nodes+1
                                //printfn "Converged nodes = %d and num_nodes = %f" converged_nodes (float(num_nodes) * 0.9)
                                return! loop()// num_nodes (converged_nodes+1) topo
        | MessageType11(num) -> 
                                printfn "------------------------ALL DONE!----------------"
                                ALL_COMPUTATIONS_DONE <- 1
        | _ -> printfn "Incorrect entry"
        return! loop()// num_nodes converged_nodes topo
    }
    loop()// 0 0 ""

// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let mySlaveActor (mailbox: Actor<_>) =
    let rec loop actor_idx rumor visited = actor {
        let! rcv = mailbox.Receive()
        let sender = mailbox.Sender()
        match rcv with
        | MessageType2(idx) ->
                        Console.WriteLine("Worker actor {0} initialized", idx)
                        return! loop idx rumor visited
        | MessageType7(rumor_string) ->
                        Console.WriteLine( "Visiting {0}", actor_idx)
                        //printfn "3"
                        //let the neighbors actor know that this actor is visited
                        neighbor_actor_ref.[0] <! MessageType5(actor_idx)
                        //call random neighbor
                        //printfn "4"
                        neighbor_actor_ref.[0] <! MessageType2(actor_idx)
                        return! loop actor_idx rumor_string (visited+1)
        | MessageType8(neighbor) ->
                        //Console.WriteLine("returned value = {0}", neighbor)
                        //printfn "6"
                        //This is the actor chosen randomly, if -1 is returned then choose a random index
                        //if an actor is returned which is techincally dead then go into that actor and 
                        //just do the same computation until a live actor is found
                        if (neighbor <> -1) then
                            slave_actor_refs.[neighbor] <! MessageType7(rumor)

                        //if -1 is returned that means this node's neighbors have converged
                        //so this must converge too
                        neighbor_actor_ref.[0] <! MessageType8(actor_idx)
                        return! loop actor_idx rumor visited
        | _ -> printfn "Incorrect entry"                
    }
    loop 0 "abc" 0

// This is the boss actor responsible for allotting parallel builds on to slaves
let myBossActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! rcv = mailbox.Receive()
        match rcv with
        | MessageType6(num_nodes, topology, algorithm) ->

                        slave_actor_refs <- Array.zeroCreate (num_nodes+1)
                        neighbor_actor_ref <- Array.zeroCreate 1

                        //Initialize the neighbors actor
                        let find_my_neighbors_actor = spawn mailbox "find_my_neighbors" find_my_neighbors
                        Array.set neighbor_actor_ref 0 find_my_neighbors_actor
                        neighbor_actor_ref.[0] <! MessageType3(num_nodes, topology)


                        printfn "Done with neighbor init"

                        // Initialize the slave actors and then store them in a refs array
                        for i in 1..num_nodes do
                            printfn "%d" i
                            let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) mySlaveActor
                            Array.set slave_actor_refs i worker_slave_actor
                            slave_actor_refs.[i] <! MessageType2(i)

                        printfn "Done with slave actors init"

                        if (algorithm = "gossip") then
                            // chossing the middle actor
                            let num = (num_nodes/2)
                            //let init_slave = spawn mailbox (sprintf "workerActor%i" num) mySlaveActor
                            //init_slave 
                            slave_actor_refs.[num] <! MessageType7("Hello")
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
    
    bossActor <! MessageType6(num_nodes, topology, algorithm)


    while(ALL_COMPUTATIONS_DONE = 0) do
        0|>ignore



    system.Terminate() |> ignore

    0
    
main fsi.CommandLineArgs
printfn "The visited array is ----"
for i in [1..num_nodes] do
    printfn "Node %d -- visited %d" i visited_actors.[i]