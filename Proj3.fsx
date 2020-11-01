
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

let mutable slave_actor_refs = Array.empty
let mutable boss_actor_ref = Array.empty
let mutable alive_nodes = Array.empty
let mutable slave_actor_keys = Map.empty
let mutable small_network_nodes = Array.empty

type Matrix<'T>(N: int, M: int) =

    let mutable internalArray = Array2D.zeroCreate<'T> N M
    
    member this.Item
            with get(a: int, b: int) = internalArray.[a, b]
            and set(a: int, b: int) (value:'T) = internalArray.[a, b] <- value

    member this.GetSlice(rowStart: int option, rowFinish : int option, colStart: int option, colFinish : int option) =
            let rowStart =
                match rowStart with
                | Some(v) -> v
                | None -> 0
            let rowFinish =
                match rowFinish with
                | Some(v) -> v
                | None -> internalArray.GetLength(0) - 1
            let colStart =
                match colStart with
                | Some(v) -> v
                | None -> 0
            let colFinish =
                match colFinish with
                | Some(v) -> v
                | None -> internalArray.GetLength(1) - 1
            internalArray.[rowStart..rowFinish, colStart..colFinish]

        member this.GetSlice(row: int, colStart: int option, colFinish: int option) =
            let colStart =
                match colStart with
                | Some(v) -> v
                | None -> 0
            let colFinish =
                match colFinish with
                | Some(v) -> v
                | None -> internalArray.GetLength(1) - 1
            internalArray.[row, colStart..colFinish]

        member this.GetSlice(rowStart: int option, rowFinish: int option, col: int) =
            let rowStart =
                match rowStart with
                | Some(v) -> v
                | None -> 0
            let rowFinish =
                match rowFinish with
                | Some(v) -> v
                | None -> internalArray.GetLength(0) - 1
            internalArray.[rowStart..rowFinish, col]

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
| ReceiveRouteMessage of Matrix<string>*string []*string*int


let timer = System.Diagnostics.Stopwatch()

let inline charToInt c = int c - int '0'

let findLongestInitialPrefix (idx1:string) (idx2:string) :int=
    let mutable prefix = 0
    for i in 0..idx1.Length do
        if idx1.[i] = idx2.[i] then
            if prefix = i then
                prefix <- prefix + 1
    prefix

let setRoutingTableEntry (route_table: Matrix<_>) (neighbor_idx: string) (longest_prefix: int)=
    if longest_prefix <> routing_table_size then
        //TODO: check that previous value is bigger in diff than current new value in that i,j place
        let next_entry_neighbor = neighbor_idx.[longest_prefix] |> charToInt
        route_table.[longest_prefix, next_entry_neighbor] <- neighbor_idx
    route_table

let findClosestEntry (route_table: Matrix<_>) (leafset: string []) (target_idx: string) (current_node_idx: string)=
    //first see if leafset contains target_idx
    let mutable return_node = ""
    for node in leafset do
        if node = target_idx then
            return_node <- target_idx
    if return_node = "" then
        let longest_prefix = findLongestInitialPrefix current_node_idx target_idx
        let node_present_or_not = route_table.[longest_prefix, int(target_idx.[longest_prefix])]
        if node_present_or_not = "" || node_present_or_not = "0" then
            //browse the leafset to find the node closes to target_idx
            let mutable min_diff = Math.Abs(Convert.ToInt32(target_idx, 4) - Convert.ToInt32(current_node_idx, 4))
            let mutable min_diff_node = ""
            let mutable diff = 0
            for node in leafset do
                if node <> "" then
                    diff <- Math.Abs(Convert.ToInt32(node, 4) - Convert.ToInt32(target_idx, 4))
                    if diff < min_diff then
                        min_diff <- diff
                        min_diff_node <- node
            return_node <- min_diff_node
        else 
            return_node <- node_present_or_not
    return_node


// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let myPastryActor (mailbox: Actor<_>) =
    let rec loop actor_idx actor_key leaf_set routing_table neighborhood_set oldtime localtime = actor {

        let! rcv = mailbox.Receive()

        match rcv with
        | PastryInit(idx, nearest_node_idx) ->

                        let n_bits = int(ceil(Math.Log(4.0, float(num_nodes))))
                        let bin_str = Convert.ToString(idx, 4).PadLeft(n_bits, '0')

                        slave_actor_keys <- slave_actor_keys.Add(bin_str, slave_actor_refs.[idx])
                        slave_actor_refs.[nearest_node_idx] <! RouteMessage(bin_str, 0)

                        let localtimer = System.Diagnostics.Stopwatch()
                        localtimer.Start()
                        return! loop idx bin_str leaf_set routing_table neighborhood_set (float(localtimer.Elapsed.TotalMilliseconds)) localtimer

        | PastryInitSmall(idx) ->

                        let self_hash = small_network_nodes.[idx]
                        let neighbor_leaf_set = small_network_nodes |> Array.filter ((<>) self_hash )
                        let mutable routing_table_self = Matrix(routing_table_size, 4)
                        for i in neighbor_leaf_set do
                            let longest_prefix = findLongestInitialPrefix i self_hash
                            routing_table_self <- setRoutingTableEntry routing_table_self i longest_prefix
                        let localtimer = System.Diagnostics.Stopwatch()
                        localtimer.Start()
                        // TODO: leafset wont have all 8 entries
                        return! loop idx self_hash neighbor_leaf_set routing_table_self neighbor_leaf_set (float(localtimer.Elapsed.TotalMilliseconds)) localtimer

        | RouteMessage(node_hash, num) ->
                        
                        let closest_node = findClosestEntry routing_table leaf_set node_hash actor_key
                        if closest_node <> "" then
                            if num = 0 then
                                slave_actor_keys.[node_hash] <! ReceiveRouteMessage(routing_table, neighborhood_set, actor_key, num)
                            else
                                slave_actor_keys.[node_hash] <! ReceiveRouteMessage(routing_table, leaf_set, actor_key, num)
                            let num_plus_one = num + 1
                            slave_actor_keys.[closest_node] <! RouteMessage(node_hash, num_plus_one)
                        else 
                            // This must mean that this is the farthest node similar to node_hash, so send the leafset
                            // also for null entries
                            slave_actor_keys.[node_hash] <! ReceiveRouteMessage(routing_table, leaf_set, actor_key, num_nodes)

                        return! loop actor_idx actor_key leaf_set routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime

        | ReceiveRouteMessage(other_routing_table, other_set, other_actor_key, num) ->

                        // set the leafset with closest entries from route table and other_set
                        let mutable new_leaf_set = leaf_set
                        for node in other_set do

                            //find the max diff between current actor hash and its own leafset
                            let mutable max_diff = Math.Abs(Convert.ToInt32(node, 4) - Convert.ToInt32(actor_key, 4))
                            let mutable max_diff_index = -1
                            let mutable counter = 0

                            for leaf in new_leaf_set do
                                if leaf <> "" then
                                    let mutable leaf_diff = Math.Abs(Convert.ToInt32(node, 4) - Convert.ToInt32(actor_key, 4))
                                    if leaf_diff > max_diff then
                                        max_diff <- leaf_diff
                                        max_diff_index <- counter
                                        counter <- counter + 1

                            if max_diff_index <> -1 then
                                new_leaf_set.[counter] <- node

                        // set routing table entires
                        let mutable new_routing_table = routing_table
                        let longest_prefix = findLongestInitialPrefix other_actor_key actor_key
                        for i in other_routing_table.[longest_prefix, *] do
                            new_routing_table <- setRoutingTableEntry new_routing_table i longest_prefix

                        if num = 0 then
                            return! loop actor_idx actor_key new_leaf_set new_routing_table other_set (float(localtime.Elapsed.TotalMilliseconds)) localtime
                        elif num = num_nodes then
                            return! loop actor_idx actor_key other_set routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime
                        else
                            return! loop actor_idx actor_key new_leaf_set new_routing_table neighborhood_set (float(localtime.Elapsed.TotalMilliseconds)) localtime

                        
        | _ -> printfn "Incorrect entry"                
    }
    loop 0 "" [||] (Matrix<string>(0, 0)) [||] 0.0 (System.Diagnostics.Stopwatch())

// This is the boss actor responsible for allotting parallel builds on to slaves
let myBossActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! rcv = mailbox.Receive()
        match rcv with
        | SendMessageBoss(max_nodes, num_requests) ->

                        num_of_requests <- num_requests
                        let max_index_2d = int(ceil(sqrt(float(max_nodes))))
                        num_nodes <- max_index_2d*max_index_2d
                        num_nodes <- max_nodes
                        routing_table_size <- Math.Log(4.0, float(num_nodes))|>int

                        slave_actor_refs <- Array.zeroCreate (num_nodes)
                        alive_nodes <- Array.zeroCreate (num_nodes)

                        //Initialize the first 8 actors to form a small network
                        small_network_nodes <- Array.zeroCreate (9)
                        let n_bits = int(ceil(Math.Log(4.0, float(num_nodes))))

                        for i in 0..9 do
                            let bin_str = Convert.ToString(i, 4).PadLeft(n_bits, '0')
                            let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) myPastryActor
                            Array.set slave_actor_refs i worker_slave_actor
                            alive_nodes.[i] <- i
                            Array.set small_network_nodes i bin_str

                        for i in 0..9 do
                            slave_actor_refs.[i] <! PastryInitSmall(i)
                        
                        // Initialize the slave actors and then store them in a refs array
                        for i in 9..num_nodes do
                            let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) myPastryActor
                            Array.set slave_actor_refs i worker_slave_actor
                            //TODO: instead of i-1, define an array with "alive nodes", and choose a random node from this array
                            slave_actor_refs.[i] <! PastryInit(i, i-1)
                            Thread.Sleep(10)
                            alive_nodes.[i] <- i 

                        printfn "Done with slave actors initialization!"

                        ALL_COMPUTATIONS_DONE <- 1
                        
                        timer.Start()


        | Deliver(msg, key) ->
                        slave_actor_keys <- slave_actor_keys.Add(msg, slave_actor_refs.[key])

        | _ -> printfn "Incorrect entry"                    
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
    boss_actor_ref.[0] <- bossActor

    bossActor <! SendMessageBoss(num_nodes, num_requests)


    while(ALL_COMPUTATIONS_DONE = 0) do
        0|>ignore

    system.Terminate() |> ignore
    0

main fsi.CommandLineArgs