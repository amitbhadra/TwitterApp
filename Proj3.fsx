
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


let mutable ALL_COMPUTATIONS_DONE = 0
let mutable num_of_requests = 0
let mutable num_nodes = 0
let n_base = 2
let mutable routing_table_size = 0
let mutable alive_counter = 0

let mutable slave_actor_refs = Array.empty
let mutable boss_actor_ref = Array.empty
let mutable alive_nodes = Array.empty
let mutable slave_actor_keys = Map.empty
let mutable small_network_nodes = Array.empty
let mutable alive_hash = Array.empty

// Define the type of messages this program can send or receive
type MyMessage =
| PastryInit of int*int
| SendMessageBoss of int*int
| Gossip of string
| SendMessageGossip of string
| SendMessagePush of double*double
| PushSum of double*double
| Ping of int
| ReceivePing of int*int
| Deliver of string*int
| PastryInitSmall of int
| RouteMessage of string*int
| ReceiveRouteMessage of string [][]*string []*string*int
| NewNodeAlert of string
| PrintMe of int

let timer = System.Diagnostics.Stopwatch()

let inline charToInt c = int c - int '0'

let findLongestInitialPrefix (idx1:string) (idx2:string) :int=
    //printfn "Entered findLongestInitialPrefix"
    let mutable prefix = 0
    if idx1 <> "" && idx2 <> "" then
        for i in 0..idx1.Length-1 do
            if idx1.[i] = idx2.[i] then
                if prefix = i then
                    prefix <- prefix + 1
        //printfn "Exit findLongestInitialPrefix found prefix = %d" prefix
            //with idx1=%s and idx2=%s and found prefix = %d" idx1 idx2 prefix
    //Console.WriteLine("Entered findLongestInitialPrefix with idx1={0} and idx2={1} and found prefix = {2}", idx1, idx2, prefix)
    prefix

let setRoutingTableEntry (route_table: string [][]) (neighbor_idx: string) (self_idx: string) (longest_prefix: int)=
    //printfn "Entered setRoutingTableEntry"
    let mutable new_routing_table = route_table
    let send_message = 
        try
            if longest_prefix < routing_table_size && neighbor_idx <> "" && self_idx <> "" then
                //check that previous value is bigger in diff than current new value in that i,j place
                let next_entry_neighbor = charToInt neighbor_idx.[longest_prefix] 
                if next_entry_neighbor <= 7 then
                    if new_routing_table.[longest_prefix].[next_entry_neighbor] = "" then
                        //printfn "longest_prefix=%d and next_entry_neighbor=%d" longest_prefix next_entry_neighbor
                        new_routing_table.[longest_prefix].[next_entry_neighbor] <- neighbor_idx
                    else
                        let mutable current_diff = Math.Abs(Convert.ToInt32(new_routing_table.[longest_prefix].[next_entry_neighbor]) - Convert.ToInt32(self_idx))
                        let mutable future_diff = Math.Abs(Convert.ToInt32(neighbor_idx) - Convert.ToInt32(self_idx))
                        if current_diff > future_diff then
                            new_routing_table.[longest_prefix].[next_entry_neighbor] <- neighbor_idx
    //printfn "Dexit setRoutingTableEntry"
        with 
            | _ as ex->
                printfn "Indexing1 Issue"
    send_message
    new_routing_table

let findClosestEntry (route_table: string [][]) (leafset: string []) (target_idx: string) (current_node_idx: string)=

    //first see if leafset contains target_idx
    Console.WriteLine("Length = {0},{1}", route_table.Length, route_table.[0].Length)
    let mutable return_node = ""
    let send_message = 
        try
            if target_idx <> "" && current_node_idx <> "" then
                for node in leafset do
                    if node = target_idx && node <> "" then
                        return_node <- target_idx
                if return_node = "" then

                    let longest_prefix = findLongestInitialPrefix current_node_idx target_idx
                    //printfn "%d %d " longest_prefix (int(target_idx.[longest_prefix]))
                    let next_digit = charToInt target_idx.[longest_prefix]
                    if next_digit > 7 || longest_prefix>2 then
                        printfn "WHOOO"
                    Console.WriteLine("{0} {1}", longest_prefix, next_digit)

                    if longest_prefix <= route_table.Length-1 && next_digit <= 7 then
                        let node_present_or_not = route_table.[longest_prefix].[next_digit]
                        if node_present_or_not = "" then
                            //browse the leafset to find the node closes to target_idx
                            let mutable min_diff = Math.Abs(Convert.ToInt32(target_idx) - Convert.ToInt32(current_node_idx))
                            let mutable min_diff_node = ""
                            let mutable diff = 0
                            for node in leafset do
                                if node <> "" then
                                    diff <- Math.Abs(Convert.ToInt32(node) - Convert.ToInt32(target_idx))
                                    if diff < min_diff then
                                        min_diff <- diff
                                        min_diff_node <- node
                            return_node <- min_diff_node
                        else 
                            return_node <- node_present_or_not
        with 
            | _ as ex->
                printfn "Indexing Issue"
    send_message
    return_node


// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let myPastryActor (mailbox: Actor<_>) =
    let rec loop actor_idx actor_key leaf_set routing_table neighborhood_set oldtime localtime = actor {

        let! rcv = mailbox.Receive()

        match rcv with
        
        | PastryInitSmall(idx) ->

                        let self_hash = small_network_nodes.[idx]
                        let neighbor_leaf_set = small_network_nodes |> Array.filter ((<>) self_hash )
                        //printfn "Pastry Init Small %d and self_hash %s" idx self_hash
                        let mutable routing_table_self = Array.create routing_table_size (Array.create 8 "")

                        for i in neighbor_leaf_set do
                            //printfn "i=%s self=%s" i self_hash
                            let longest_prefix = findLongestInitialPrefix i self_hash
                            if longest_prefix < routing_table_size then
                                routing_table_self <- setRoutingTableEntry routing_table_self i self_hash longest_prefix
                        let localtimer = System.Diagnostics.Stopwatch()
                        //localtimer.Start()
                        //printfn "Pastry Init Small %d fin" idx
                        let sender = mailbox.Sender()
                        sender <! "Pastry Init Small fin"
                        // TODO: leafset wont have all 8 entries - it should have 4 smaller and 4 larger
                        return! loop idx self_hash neighbor_leaf_set routing_table_self neighbor_leaf_set (float(localtimer.Elapsed.TotalMilliseconds)) localtimer

        | PastryInit(idx, nearest_node_idx) ->

                        let mutable random_node = new System.Random()
                        let mutable random_num_node = random_node.Next(0, alive_hash.Length)

                        alive_hash <- alive_hash |> Array.filter ((<>) random_num_node )
                        let n_bits = int(ceil(Math.Log(float(num_nodes), 8.0)))
                        let bin_str_node = Convert.ToString(random_num_node, 8).PadLeft(n_bits, '0')
                        //printfn "Pastry Init  = %d nearest_node_idx = %d random_num = %d random_hash = %s" idx nearest_node_idx random_num_node bin_str_node
                        slave_actor_keys <- slave_actor_keys.Add(bin_str_node, slave_actor_refs.[idx])

                        //printfn "Gone here"
                        let mutable routing_table_self = Array.create routing_table_size (Array.create 8 "")
                        let mutable leafset = Array.create 8 ""
                        let mutable neighborhoodset = Array.create 8 ""
                        let localtimer = System.Diagnostics.Stopwatch()
                        localtimer.Start()
                        //printfn "Pastry Init Small %d fin" idx
                        let sender = mailbox.Sender()
                        sender <! "Pastry Init Small fin"
                        slave_actor_refs.[nearest_node_idx] <! RouteMessage(bin_str_node, 0)
                        return! loop idx bin_str_node leafset routing_table_self neighborhoodset (float(localtimer.Elapsed.TotalMilliseconds)) localtimer

        | RouteMessage(node_hash, num) ->

                //let send_message = 
                    //try
                        if actor_key = "" then
                            ////printfn "NOOOOOO %d %s" actor_idx node_hash
                            Console.WriteLine("NOOOOOO {0} {1}", actor_idx, node_hash)
                        else 
                            //printfn "YESSSSS %d %s" actor_idx node_hash
                        //printfn "Entered RouteMessage with actor_idx=%d actor_key=%s node_hash = %s num = %d" actor_idx actor_key node_hash num
                        let closest_node = findClosestEntry routing_table leaf_set node_hash actor_key
                        if closest_node <> "" then
                            if num = 0 then
                                //printfn "START1: node_hash = %s num = %d" node_hash num
                                slave_actor_keys.[node_hash] <! ReceiveRouteMessage(routing_table, neighborhood_set, actor_key, num)
                                //printfn "END1: node_hash = %s num = %d" node_hash num
                            else
                                //printfn "START2: node_hash = %s num = %d" node_hash num
                                slave_actor_keys.[node_hash] <! ReceiveRouteMessage(routing_table, leaf_set, actor_key, num)
                                //printfn "END2: node_hash = %s num = %d" node_hash num
                            let num_plus_one = num + 1
                            slave_actor_keys.[closest_node] <! RouteMessage(node_hash, num_plus_one)
                            //printfn "END3: node_hash = %s num = %d" node_hash num
                        else 
                            // This must mean that this is the farthest node similar to node_hash, so send the leafset
                            // also for null entries
                            slave_actor_keys.[node_hash] <! ReceiveRouteMessage(routing_table, leaf_set, actor_key, num_nodes)
                        //printfn "END4: node_hash = %s num = %d" node_hash num

                        return! loop actor_idx actor_key leaf_set routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime
                    //with 
                        //| _ as ex->
                            ////printfn "Neighbor deleted by some other thread"
                //send_message
                //return! loop actor_idx actor_key leaf_set routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime

        | ReceiveRouteMessage(other_routing_table, other_set, other_actor_key, num) ->

                        if actor_key = "" then
                            printfn "N1OOOOOO %d" actor_idx
                        else 
                            printfn "Y1ESSSSS %d" actor_idx
                        // set the leafset with closest entries from route table and other_set
                        let mutable new_leaf_set = leaf_set
                        for node in other_set do
                            if node <> "" && actor_key <> "" then
                                //find the max diff between current actor hash and its own leafset
                                let mutable max_diff = Math.Abs(Convert.ToInt32(node) - Convert.ToInt32(actor_key))
                                let mutable max_diff_index = -1
                                let mutable counter = 0

                                for leaf in new_leaf_set do
                                    if leaf <> "" then
                                        let mutable leaf_diff = Math.Abs(Convert.ToInt32(node) - Convert.ToInt32(actor_key))
                                        if leaf_diff > max_diff then
                                            max_diff <- leaf_diff
                                            max_diff_index <- counter
                                            counter <- counter + 1

                                if max_diff_index <> -1 then
                                    new_leaf_set.[counter] <- node

                        // set routing table entires
                        let mutable new_routing_table = routing_table
                        let longest_prefix = findLongestInitialPrefix other_actor_key actor_key

                        if longest_prefix < routing_table_size then
                            for i in other_routing_table.[longest_prefix] do
                                new_routing_table <- setRoutingTableEntry new_routing_table i actor_key longest_prefix

                        if num = 0 then
                            return! loop actor_idx actor_key new_leaf_set new_routing_table other_set (float(localtime.Elapsed.TotalMilliseconds)) localtime
                        elif num = num_nodes then
                            // we must send back information about current node to all the nodes in the routing table
                            for i in 0..routing_table.Length-1 do
                                for j in 0..routing_table.[i].Length-1 do
                                    if routing_table.[i].[j] <> "" then
                                        slave_actor_keys.[routing_table.[i].[j]] <! NewNodeAlert(actor_key)
                            //printfn "Done with %d" actor_idx
                            alive_nodes.[actor_idx] <- actor_idx 
                            alive_counter <- alive_counter + 1
                            return! loop actor_idx actor_key other_set routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime
                        else
                            return! loop actor_idx actor_key new_leaf_set new_routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime

        | NewNodeAlert (new_node_hash) ->

                        if actor_key = "" then
                            printfn "NOOOOOO %d" actor_idx
                        // change routing table entry based on new node alert
                        let mutable new_routing_table = routing_table
                        let longest_prefix = findLongestInitialPrefix new_node_hash actor_key
                        new_routing_table <- setRoutingTableEntry new_routing_table new_node_hash actor_key longest_prefix
                        return! loop actor_idx actor_key leaf_set new_routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime

        | PrintMe(idx) ->
                        Console.WriteLine("For idx={0} ", idx)
                        printfn "%A" routing_table
                        printfn "%A" leaf_set
                        let sender = mailbox.Sender()
                        sender <! "done"
                        return! loop actor_idx actor_key leaf_set routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime

        | _ -> printfn "Incorrect entry"                
    }
    loop 0 "" [||] [||] [||] 0.0 (System.Diagnostics.Stopwatch())

// This is the boss actor responsible for allotting parallel builds on to slaves
let myBossActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! rcv = mailbox.Receive()
        match rcv with
        | SendMessageBoss(max_nodes, num_requests) ->

                        num_of_requests <- num_requests
                        //let max_index_2d = int(ceil(sqrt(float(max_nodes))))
                        //num_nodes <- max_index_2d*max_index_2d
                        num_nodes <- max_nodes
                        routing_table_size <- ceil(Math.Log(float(num_nodes), 8.0) + 1.0) |>int 

                        //printfn "4"
                        slave_actor_refs <- Array.zeroCreate (num_nodes)
                        alive_nodes <- Array.zeroCreate (num_nodes)
                        //printfn "5"
                        alive_hash <- Array.zeroCreate (num_nodes)
                        //Initialize alive hash array
                        for i in 0..num_nodes-1 do
                            alive_hash.[i] <- i
                        let mutable random = new System.Random()

                        //printfn "6"
                        //Initialize the first 8 actors to form a small network
                        small_network_nodes <- Array.zeroCreate (9)
                        let n_bits = int(ceil(Math.Log(float(num_nodes), 8.0)))
                        //printfn "alive has length = %d and n_bits = %d and num_nodes = %d" alive_hash.Length n_bits num_nodes
                        //printfn "7"
                        for i in 0..8 do
                            let mutable random_num = random.Next(0, alive_hash.Length)
                            //printfn "%d" random_num
                            alive_hash <- alive_hash |> Array.filter ((<>) random_num )
                            let bin_str = Convert.ToString(random_num, 8).PadLeft(n_bits, '0')
                            let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) myPastryActor
                            slave_actor_refs.[i] <- worker_slave_actor
                            slave_actor_keys <- slave_actor_keys.Add(bin_str, slave_actor_refs.[i])
                            alive_nodes.[i] <- random_num
                            alive_counter <- alive_counter + 1
                            small_network_nodes.[i] <- bin_str

                        //printfn "Done8"

                        //printfn "%A" small_network_nodes
                        //printfn "%A" slave_actor_keys

                        for i in 0..8 do
                            //slave_actor_refs.[i] <! PastryInitSmall(i)
                            let mutable res = slave_actor_refs.[i] <? PastryInitSmall(i)
                            let mutable sync = Async.RunSynchronously(res, 100) |> string
                            0|>ignore
                            //Thread.Sleep(100)
                        //printfn "9"
                        //printfn "Done with first 9"
                        
                        // Initialize the slave actors and then store them in a refs array
                        for i in 9..num_nodes-1 do
                            let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) myPastryActor
                            slave_actor_refs.[i] <- worker_slave_actor
                            let mutable random_num = random.Next(0, alive_counter)
                            let mutable res = slave_actor_refs.[i] <? PastryInit(i, random_num)
                            let mutable sync = Async.RunSynchronously(res, 100) |> string
                            0|>ignore
                            //Thread.Sleep(100)


                        //printfn "Done with slave actors initialization!"
                        for i in 0..num_nodes-1 do
                            let mutable res = slave_actor_refs.[i] <? PrintMe(i)
                            let mutable sync = Async.RunSynchronously(res, 1000) |> string
                            sync|>ignore
                            //0|>ignore

                        printfn "Map: %A" slave_actor_keys

                        ALL_COMPUTATIONS_DONE <- 1
                        
                        timer.Start()
                        
        | _ -> //printfn "Incorrect entry"                    
        return! loop()
    }
    loop()


// main function used to take in parameters
let main argv =
    let num_nodes = ((Array.get argv 1) |> int)
    let num_requests = ((Array.get argv 2) |> int)

    let system = System.create "system" <| ConfigurationFactory.Default()

    let bossActor = spawn system "boss_actor" myBossActor

    boss_actor_ref <- Array.zeroCreate 1
    //printfn "1"
    boss_actor_ref.[0] <- bossActor
    //printfn "2"
    boss_actor_ref.[0] <! SendMessageBoss(num_nodes, num_requests)
    //printfn "3"

    while(ALL_COMPUTATIONS_DONE = 0) do
        0|>ignore

    system.Terminate() |> ignore
    0

main fsi.CommandLineArgs