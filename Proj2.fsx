
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

let stopWatch = System.Diagnostics.Stopwatch.StartNew()

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
let mutable converged_actors = Array.empty
let mutable visited_actors = Array.empty
let mutable boss_actor_ref = Array.empty
let mutable converged_nodes = 0
let mutable topo = ""
let mutable num_nodes = 0

let converged_nodes_array idx = 
    //printfn "Reached Converged Nodes"
    if(converged_actors.[idx] = 0) then
        printfn "Converged %d" idx
        converged_actors.[idx] <- 1
        converged_nodes <- converged_nodes + 1
        if(converged_nodes = num_nodes) then
            printfn "------------------------ALL DONE------------------------"                                   
            ALL_COMPUTATIONS_DONE <- 1
            exit(0)
        //printfn "Starting %d to %d" idx (idx+1)
        if(idx <> num_nodes) then
            //printfn "Sending %d" (idx+1)
            slave_actor_refs.[idx+1] <! MessageType13(idx)
        //printfn "Starting %d to %d" idx (idx-1)
        if(idx <> 1) then
            //printfn "Sending %d" (idx-1)
            slave_actor_refs.[idx-1] <! MessageType14(idx)

        


// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let mySlaveActor (mailbox: Actor<_>) =
    let rec loop actor_idx rumor visited neighbors = actor {

        let! rcv = mailbox.Receive()

        match rcv with
        | MessageType2(idx) ->
                        ////Console.WriteLine("Worker actor {0} initialized", idx)
                        if (topo = "line") then
                            let mutable n_left = find_left_neighbor idx num_nodes
                            let mutable n_right = find_right_neighbor idx num_nodes
                            let mutable return_array = Array.create 1 -1
                            //if (n_left > 0) then
                            return_array <- [|n_left|]
                            //if (n_right > 0) then
                            return_array <- Array.append return_array [|n_right|]
                            //Console.WriteLine("Worker actor {0} initialized with values ", idx)
                            //printfn "Worker actor %d initialized with values %A" idx return_array
                            return! loop idx rumor visited return_array
                        if (topo = "full") then
                            let mutable return_array = Array.create (num_nodes+1) -1
                            for i in 1..num_nodes do
                                return_array.[i] <- i
                            return! loop idx rumor visited return_array
        | MessageType13(idx) ->
                        let sender = mailbox.Sender()
                        let mutable new_nbr =  Array.append [|idx-1|] [|neighbors.[1]|]
                        //printf "For %d new neighbors = %A" actor_idx new_nbr
                        //sender <! 0
                        return! loop idx rumor visited new_nbr
        | MessageType14(idx) ->
                        let sender = mailbox.Sender()
                        let mutable new_nbr =  Array.append [|neighbors.[0]|] [|idx+1|]
                        //printf "For %d new neighbors = %A" actor_idx new_nbr
                        //sender <! 0
                        return! loop idx rumor visited new_nbr
        | MessageType12(rumor_string) ->
                        if (visited_actors.[actor_idx] < 10) then
                            slave_actor_refs.[actor_idx] <! MessageType7(rumor_string)
                            return! loop actor_idx rumor_string (visited+1) neighbors
                        else
                            converged_nodes_array actor_idx
                            return! loop actor_idx rumor_string visited neighbors
        | MessageType7(rumor_string) ->
                        if(visited_actors.[actor_idx] >=10) then
                            boss_actor_ref.[0] <! MessageType2(actor_idx)
                            //converged_nodes_array actor_idx
                        //Console.WriteLine( "Visiting {0} with visited {1}", actor_idx, visited_actors.[actor_idx])
                        //printfn "Visiting %d with visited %d" actor_idx visited
                        //printfn "3"

                        //if (visited >= 10) then
                        //    converged_nodes_array actor_idx
                        //    return! loop actor_idx rumor (visited+1) neighbors

                        //Console.WriteLine( "Visiting {0} with visited {1}", actor_idx, visited)
                        //printfn "Visiting %d from %d with visited %d" actor_idx previous_idx visited

                        if (topo = "line") then
                            //Console.WriteLine( "Visiting {0} with visited {1}", actor_idx, visited)
                            //Console.WriteLine("Left found - {0}", n_left)
                            //Console.WriteLine("Right found - {0}", n_right)

                            ////Console.WriteLine("Neighbors Array : ")
                            ////Console.WriteLine("[{0}]", String.Join(", ", return_array))
                            //while(visited_actors.[actor_idx] <= 10) do
                            
                                //printfn "%d" return_array.[random_idx]
                                //Console.WriteLine("At {0} Visiting {1} with visited {2}",actor_idx, return_array.[random_idx], visited )
                                //if(actor_idx = 10) then
                                //    Console.WriteLine("{0} and {1}" , return_array.[random_idx], array_index)

                            while(visited_actors.[actor_idx] <=10) do

                                let mutable random = new System.Random()

                                let mutable random_idx = random.Next(0, neighbors.Length)


                                while (neighbors.[random_idx] = -1) do
                                    random_idx <- random.Next(0, neighbors.Length)

         //printfn "while running.."

                                slave_actor_refs.[neighbors.[random_idx]] <! MessageType12(rumor)
                                visited_actors.[actor_idx] <- (visited_actors.[actor_idx] + 1)

                                return! loop actor_idx rumor_string (visited+1) neighbors
                                        //Async.Sleep(10) |> Async.RunSynchronously
                            boss_actor_ref.[0] <! MessageType2(actor_idx)
                                //printfn "At %d called %d with visited %d" actor_idx return_array.[random_idx] visited
                                //Console.WriteLine( "At {2} Visited {0} with visited {1}", return_array.[random_idx], visited, actor_idx)

                            //converged_nodes_array actor_idx

                        if (topo = "full") then
                            while(visited_actors.[actor_idx] <=10) do

                                let mutable random = new System.Random()

                                let mutable random_idx = random.Next(0, neighbors.Length)


                                while (neighbors.[random_idx] = -1) do
                                    random_idx <- random.Next(0, neighbors.Length)
                                //printfn "Random neighbor = %d" neighbors.[random_idx]
                                slave_actor_refs.[neighbors.[random_idx]] <! MessageType12(rumor)
                                visited_actors.[actor_idx] <- (visited_actors.[actor_idx] + 1)

                                //return! loop actor_idx rumor_string (visited+1) neighbors
                                        //Async.Sleep(10) |> Async.RunSynchronously
                            boss_actor_ref.[0] <! MessageType2(actor_idx)
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

                        slave_actor_refs <- Array.zeroCreate (num_nodes+1)
                        converged_actors <- Array.zeroCreate (num_nodes+1)
                        visited_actors <- Array.zeroCreate (num_nodes+1)
                        

                        printfn "Done with neighbor init"

                        // Initialize the slave actors and then store them in a refs array
                        for i in 1..num_nodes do
                            //printfn "%d" i
                            let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) mySlaveActor
                            Array.set slave_actor_refs i worker_slave_actor
                            slave_actor_refs.[i] <! MessageType2(i)

                        printfn "Done with slave actors init"

                        if (algorithm = "gossip") then
                            // chossing the middle actor
                            let num = (num_nodes/2)
                            //let init_slave = spawn mailbox (sprintf "workerActor%i" num) mySlaveActor
                            //init_slave 
                            slave_actor_refs.[num] <! MessageType12("Hello")
        | MessageType2(idx) ->
                        if(converged_actors.[idx] = 0) then
                            printfn "Converged %d" idx
                            converged_actors.[idx] <- 1
                            converged_nodes <- converged_nodes + 1
                            printfn "Total finished %d" converged_nodes
                            if(float(converged_nodes) >= (float(num_nodes)* 0.80 )) then
                                printfn "------------------------ALL DONE------------------------"
                                stopWatch.Stop()
                                printfn "%f" stopWatch.Elapsed.TotalMilliseconds                                   
                                ALL_COMPUTATIONS_DONE <- 1
                                exit(0)
                            //printfn "Starting %d to %d" idx (idx+1)
                            if(idx <> num_nodes) then
                                //printfn "Sending %d" (idx+1)
                                slave_actor_refs.[idx+1] <! MessageType13(idx)
                            //printfn "Starting %d to %d" idx (idx-1)
                            if(idx <> 1) then
                                //printfn "Sending %d" (idx-1)
                                slave_actor_refs.[idx-1] <! MessageType14(idx)
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
//printfn "The visited array is ----"
//for i in [1..num_nodes] do
//    printfn "Node %d -- visited %d" i visited_actors.[i]