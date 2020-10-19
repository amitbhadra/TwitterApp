
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
| GetNeighbors of int
| SendMessageBoss of int*string*string*Akka.Actor.ActorSystem
| Gossip of string
| SendMessageGossip of string
| SendMessagePush of double*double
| PushSum of double*double
| Ping of int
| ReceivePing of int*int

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
let convergence = 10


let converged_nodes_array idx rumor= 
    if(converged_actors.[idx] = 0) then
        printfn "Converged %d" idx
        converged_actors.[idx] <- 1
        alive_nodes <- alive_nodes |> Array.filter ((<>)idx)
        converged_nodes <- converged_nodes + 1
        printfn "Total finished %d" converged_nodes
    if(alive_nodes.Length = 1 || float(converged_nodes) >= 0.99*float(num_nodes)) then
        printfn "------------------------ALL DONE------------------------"
        timer.Stop()
        printfn "%f" timer.Elapsed.TotalMilliseconds                                   
        ALL_COMPUTATIONS_DONE <- 1
        exit(0)
    let mutable random = new System.Random()
    let mutable new_random_idx = random.Next(1, alive_nodes.Length)
    slave_actor_refs.[alive_nodes.[new_random_idx]] <! SendMessageGossip(rumor)


let converged_nodes_array_push_sum idx= 
    if(converged_actors.[idx] = 0) then
        printfn "Converged %d" idx
        converged_actors.[idx] <- 1
        alive_nodes <- alive_nodes |> Array.filter ((<>)idx)
        converged_nodes <- converged_nodes + 1
    if(alive_nodes.Length = 1 || converged_nodes >= num_nodes) then
        printfn "------------------------ALL DONE------------------------"
        timer.Stop()
        printfn "%f" timer.Elapsed.TotalMilliseconds                                   
        ALL_COMPUTATIONS_DONE <- 1
        exit(0)
    let mutable random = new System.Random()
    let mutable new_random_idx = random.Next(1, alive_nodes.Length)
    slave_actor_refs.[alive_nodes.[new_random_idx]] <! SendMessagePush(0.0, 0.0)

    
let findIndex arr elem = arr |> Array.findIndex ((=) elem)


// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let mySlaveActor (mailbox: Actor<_>) =
    let rec loop actor_idx rumor visited neighbors alive_status num_pings = actor {

        let! rcv = mailbox.Receive()

        match rcv with
        | GetNeighbors(idx) ->

                        if (topo = "line") then
                            let mutable return_array = Array.create 2 -1
                            return_array.[0] <- find_left_neighbor idx num_nodes
                            return_array.[1] <- find_right_neighbor idx num_nodes
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            let mutable alive_status_array = Array.create return_array.Length 0
                            let mutable num_pings_array = Array.create return_array.Length 0
                            return! loop idx rumor visited return_array alive_status_array num_pings_array

                        if (topo = "full") then
                            let mutable return_array = Array.create (num_nodes+1) -1
                            for i in 1..num_nodes do
                                return_array.[i] <- i
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            let mutable alive_status_array = Array.create return_array.Length 0
                            let mutable num_pings_array = Array.create return_array.Length 0
                            return! loop idx rumor visited return_array alive_status_array num_pings_array

                        if (topo = "2D" || topo = "imp2D") then
                            let mutable return_array = Array.create 4 -1
                            return_array.[0] <- find_down_neighbor_2d idx max_index_2d
                            return_array.[1] <- find_up_neighbor_2d idx max_index_2d
                            return_array.[2] <- find_right_neighbor_2d idx max_index_2d
                            return_array.[3] <- find_left_neighbor_2d idx max_index_2d
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            let mutable alive_status_array = Array.create return_array.Length 0
                            let mutable num_pings_array = Array.create return_array.Length 0
                            return! loop idx rumor visited return_array alive_status_array num_pings_array

        | Ping(sender_idx) ->

                        let mutable random = new System.Random()
                        let mutable random_num = random.Next(0, 500)
                        if(random_num >= 0 && random_num <=496) then
                            slave_actor_refs.[sender_idx] <! ReceivePing(actor_idx, 1)
                        elif (random_num >= 497 && random_num <=498) then
                            slave_actor_refs.[sender_idx] <! ReceivePing(actor_idx, 0)
                        else
                            slave_actor_refs.[sender_idx] <! ReceivePing(actor_idx, -1)
                        return! loop actor_idx rumor visited neighbors alive_status num_pings

        | ReceivePing(node_idx, node_status) ->

                        //If node_status = 1 it means node_idx is alive
                        //If node_status = 0 it means node_idx is temporarily dead
                        //If node_status = -1 it means node_idx is permanently dead

                        let mutable neighbor_array = neighbors 
                        let mutable alive_status_array = alive_status 
                        let mutable num_pings_array = num_pings

                        let find_index elem =
                            try
                                findIndex neighbors node_idx
                            with
                                | :? System.Collections.Generic.KeyNotFoundException -> 
                                    -1
                        let mutable neighbor_relative_idx = find_index node_idx

                        //printfn "Searching for %d in %A where actor is %d" node_idx neighbors actor_idx
                        //printfn "Relative index is %d" neighbor_relative_idx

                        if(neighbor_relative_idx <> -1) then
                            let update_neighbor_status = 
                                try
                                    if(node_status = 1) then
                                        num_pings.[neighbor_relative_idx] <- 0
                                        alive_status.[neighbor_relative_idx] <- 1

                                    elif(node_status = 0) then
                                        // find the idx of node_idx in the arrays
                                        num_pings_array.[neighbor_relative_idx] <- num_pings_array.[neighbor_relative_idx] + 1
                                        alive_status_array.[neighbor_relative_idx] <- -1

                                        if(num_pings_array.[neighbor_relative_idx] >= 5) then
                                            printfn "Connection to %d and %d is permanently dead!" node_idx actor_idx

                                            neighbor_array.[neighbor_relative_idx] <- -1
                                            alive_status_array.[neighbor_relative_idx] <- -1
                                            num_pings_array.[neighbor_relative_idx] <- -1

                                            neighbor_array <- neighbor_array |> Array.filter ((<>) -1 )
                                            alive_status_array <- alive_status_array |> Array.filter ((<>) -1) 
                                            num_pings_array <- num_pings_array |> Array.filter ((<>) -1)

                                        else
                                            printfn "Connection to %d and %d is temporarily blocked" node_idx actor_idx

                                    else
                                        printfn "Node %d is dead!" node_idx
                                        neighbor_array.[neighbor_relative_idx] <- -1
                                        alive_status_array.[neighbor_relative_idx] <- -1
                                        num_pings_array.[neighbor_relative_idx] <- -1

                                        neighbor_array <- neighbor_array |> Array.filter ((<>) -1 )
                                        alive_status_array <- alive_status_array |> Array.filter ((<>) -1) 
                                        num_pings_array <- num_pings_array |> Array.filter ((<>) -1)
                                        visited_actors.[node_idx] <- 10
                                        converged_nodes_array node_idx rumor
                                with 
                                    | _ as ex->
                                        printfn "Neighbor deleted by some other thread"

                            update_neighbor_status

                        return! loop actor_idx rumor visited neighbor_array alive_status_array num_pings_array


        | SendMessageGossip(rumor_string) ->

                        if (visited_actors.[actor_idx] < convergence) then
                            visited_actors.[actor_idx] <- (visited_actors.[actor_idx] + 1)
                            slave_actor_refs.[actor_idx] <! Gossip(rumor_string)
                            return! loop actor_idx rumor_string (visited+1) neighbors alive_status num_pings
                        else
                            converged_nodes_array actor_idx rumor
                            return! loop actor_idx rumor_string visited neighbors alive_status num_pings

        | Gossip(rumor_string) ->
                        let send_message = 
                            try
                                for i in neighbors do
                                    slave_actor_refs.[i] <! Ping(actor_idx)

                                if(visited_actors.[actor_idx] >= convergence) then
                                    converged_nodes_array actor_idx rumor
                                else
                                    //Console.WriteLine("Visiting {0}", actor_idx)
                                    if(topo = "line") then
                                        Thread.Sleep(5)
                                        //Async.Sleep(5) |> Async.RunSynchronously
                                    let mutable random = new System.Random()

                                    let mutable advanced_neighbors = neighbors

                                    if(topo = "imp2D") then
                                        let mutable all_actors = [|1..num_nodes|]
                                        for i in 0..advanced_neighbors.Length-1 do
                                            all_actors <- all_actors |> Array.filter ((<>) advanced_neighbors.[i] )
                                        let mutable random_all_actors_idx = random.Next(0, all_actors.Length)
                                        advanced_neighbors <- Array.append advanced_neighbors [|all_actors.[random_all_actors_idx]|]

                                    let mutable random_idx = random.Next(0, advanced_neighbors.Length)

                                    // iterate Math.Max(1000, num_nodes*3 times over the neighbors and if all are visited then choose a random number
                                    // neighbors.Length * 2 is chosen because we don't want the loop to be ever-lasting and it seems like a 
                                    // reasonable enough value
                                    let mutable iterator = 0

                                    while (visited_actors.[advanced_neighbors.[random_idx]] < convergence && iterator <= Math.Max(1000, num_nodes*3)) do
                                        random_idx <- random.Next(0, advanced_neighbors.Length)
                                        iterator <- iterator + 1

                                    if(visited_actors.[advanced_neighbors.[random_idx]] < convergence) then
                                        slave_actor_refs.[advanced_neighbors.[random_idx]] <! SendMessageGossip(rumor)
                                    else
                                        let mutable random = new System.Random()
                                        let mutable new_random_idx = random.Next(1, alive_nodes.Length)
                                        slave_actor_refs.[new_random_idx] <! SendMessageGossip(rumor)

                                        let mutable sleep_itr = 0
                                        while(sleep_itr < 1000) do
                                            sleep_itr <- (sleep_itr + 1)

                                    slave_actor_refs.[actor_idx] <! Gossip(rumor)

                            with 
                                | _ as ex->
                                    printfn "Neighbor deleted by some other thread"
                        send_message
                        return! loop actor_idx rumor_string (visited+1) neighbors alive_status num_pings
        | _ -> printfn "Incorrect entry"                
    }
    loop 0 "abc" 0 [||] [||] [||]


// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let myPushSumActor (mailbox: Actor<_>) =
    let rec loop actor_idx state_1 state_2 state_3 s w neighbors = actor {

        let! rcv = mailbox.Receive()

        match rcv with
        | GetNeighbors(idx) ->

                        if (topo = "line") then
                            let mutable return_array = Array.create 2 -1
                            return_array.[0] <- find_left_neighbor idx num_nodes
                            return_array.[1] <- find_right_neighbor idx num_nodes
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            return! loop idx state_1 state_2 state_3 (double(idx)) 1.0 return_array

                        if (topo = "full") then
                            let mutable return_array = Array.create (num_nodes+1) -1
                            for i in 1..num_nodes do
                                return_array.[i] <- i
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            return! loop idx state_1 state_2 state_3 (double(idx)) 1.0 return_array

                        if (topo = "2D" || topo = "imp2D") then
                            let mutable return_array = Array.create 4 -1
                            return_array.[0] <- find_down_neighbor_2d idx max_index_2d
                            return_array.[1] <- find_up_neighbor_2d idx max_index_2d
                            return_array.[2] <- find_right_neighbor_2d idx max_index_2d
                            return_array.[3] <- find_left_neighbor_2d idx max_index_2d
                            return_array <- return_array |> Array.filter ((<>) -1 )
                            return! loop idx state_1 state_2 state_3 (double(idx)) 1.0 return_array

        | SendMessagePush(new_s, new_w) ->

                        if (converged_actors.[actor_idx] = 0) then
                            slave_actor_refs.[actor_idx] <! PushSum(new_s, new_w)
                            return! loop actor_idx  state_1 state_2 state_3 s w neighbors
                        else
                            converged_nodes_array_push_sum actor_idx
                            return! loop actor_idx  state_1 state_2 state_3 s w neighbors

        | PushSum(new_s, new_w) ->

                        let mutable updated_s = s + new_s
                        let mutable updated_w = w + new_w
                        let mutable ratio = updated_s / updated_w

                        if (actor_idx = num_nodes/2) then
                            Console.WriteLine("Ratio = {0} State_1 = {1}", double(ratio), double(state_2))

                        if(abs(ratio - state_2) < 0.00000000001) then
                            if (actor_idx = num_nodes/2) then
                                Console.WriteLine("Ratio = {0} State_1 = {1}", double(ratio), double(state_2))
                            converged_nodes_array_push_sum actor_idx
                            return! loop actor_idx  state_1 state_2 state_3 s w neighbors
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

                            // iterate Math.Max(1000, num_nodes*3) times over the neighbors and if all are visited then choose a random number
                            // neighbors.Length * 2 is chosen because we don't want the loop to be ever-lasting and it seems like a 
                            // reasonable enough value
                            let mutable iterator = 0

                            while (converged_actors.[advanced_neighbors.[random_idx]] = 1 && iterator <= Math.Max(1000, num_nodes*3)) do
                                random_idx <- random.Next(0, advanced_neighbors.Length)
                                iterator <- iterator + 1

                            if(visited_actors.[advanced_neighbors.[random_idx]] = 0) then
                                slave_actor_refs.[advanced_neighbors.[random_idx]] <! SendMessagePush(updated_s/2.0, updated_w/2.0)
                            else
                                let mutable random = new System.Random()
                                let mutable new_random_idx = random.Next(1, alive_nodes.Length)
                                slave_actor_refs.[new_random_idx] <! SendMessagePush(updated_s/2.0, updated_w/2.0)

                            return! loop actor_idx state_2 state_3 ratio (updated_s/2.0) (updated_w/2.0) neighbors
        | _ -> printfn "Incorrect entry"                
    }
    loop 0 0.0 0.0 0.0 0.0 0.0 [||]

// This is the boss actor responsible for allotting parallel builds on to slaves
let myBossActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! rcv = mailbox.Receive()
        match rcv with
        | SendMessageBoss(max_nodes, topology, algorithm, system) ->

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
                        
                        if (algorithm = "gossip") then
                            for i in 1..num_nodes do
                                let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) mySlaveActor
                                Array.set slave_actor_refs i worker_slave_actor
                                slave_actor_refs.[i] <! GetNeighbors(i)
                                alive_nodes.[i] <- i 
                        else if(algorithm = "push-sum") then
                            for i in 1..num_nodes do
                                let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) myPushSumActor
                                Array.set slave_actor_refs i worker_slave_actor
                                slave_actor_refs.[i] <! GetNeighbors(i)
                                alive_nodes.[i] <- i 

                        printfn "Done with slave actors initialization!"
                        
                        timer.Start()

                        if (algorithm = "gossip") then
                            slave_actor_refs.[1] <! SendMessageGossip("Hello")
                        else if(algorithm = "push-sum") then
                            slave_actor_refs.[num_nodes/2] <! SendMessagePush(0.0, 0.0)
                        
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

    bossActor <! SendMessageBoss(num_nodes, topology, algorithm, system)


    while(ALL_COMPUTATIONS_DONE = 0) do
        0|>ignore

    system.Terminate() |> ignore
    0

main fsi.CommandLineArgs