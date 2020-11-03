
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
| Init of int

type Arr2D = 
| Array2D of string[][]

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
                Console.WriteLine("{0}", next_entry_neighbor)
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
                //printfn "Indexing1 Issue"
                printf ""
    send_message
    new_routing_table

let findClosestSmallLeaf (tempset: string[]) (node_hash: string) :string=
    // this finds the closest small leaf in leafset from node_hash
    let mutable res = ""
    if node_hash <> "" then
        let mutable min_diff_abs = num_nodes
        let mutable min_diff = num_nodes
        let mutable leaf = ""
        let mutable diff = min_diff
        let mutable diff_abs = min_diff_abs
        for l in 0..tempset.Length-1 do
            leaf <- tempset.[l]
            if leaf <> "" then
                diff <- Convert.ToInt32(leaf) - Convert.ToInt32(node_hash)
                diff_abs <- Math.Abs(Convert.ToInt32(leaf) - Convert.ToInt32(node_hash))
                if diff < 0 && diff_abs < min_diff_abs then
                    res <- leaf
                    min_diff_abs <- diff_abs
    res

let findClosestBigLeaf (tempset: string[]) (node_hash: string) :string=
    // this finds the closest big leaf in leafset from node_hash
    let mutable res = ""
    if node_hash <> "" then
        let mutable min_diff_abs = num_nodes
        let mutable min_diff = num_nodes
        let mutable leaf = ""
        let mutable diff = min_diff
        let mutable diff_abs = min_diff_abs
        for l in 0..tempset.Length-1 do
            leaf <- tempset.[l]
            if leaf <> "" then
                diff <- Convert.ToInt32(leaf) - Convert.ToInt32(node_hash)
                diff_abs <- Math.Abs(Convert.ToInt32(leaf) - Convert.ToInt32(node_hash))
                if diff > 0 && diff_abs < min_diff_abs then
                    res <- leaf
                    min_diff_abs <- diff_abs
    res


// This is the slave actor which will take in a number N and k, and return N if its a perfect number or 0 of not
let myPastryActor (mailbox: Actor<_>) =
    let rec loop actor_idx actor_key leaf_set routing_table neighborhood_set oldtime localtime = actor {

        let! rcv = mailbox.Receive()

        match rcv with
        
        | PastryInitSmall(idx) ->

                        let self_hash = small_network_nodes.[idx]
                        let mutable neighbor_leaf_set = small_network_nodes |> Array.filter ((<>) self_hash )
                        //printfn "Pastry Init Small %d and self_hash %s" idx self_hash
                        let mutable routing_table_self = Array.create routing_table_size (Array.create 8 "")
                        let mutable RT = Array2D.init routing_table_size 8 (fun i j -> "")
                        let mutable leafset = Array.zeroCreate 8

                        for i in neighbor_leaf_set do
                            //printfn "i=%s self=%s" i self_hash
                            let longest_prefix = findLongestInitialPrefix i self_hash
                            if longest_prefix < routing_table_size then
                                routing_table_self <- setRoutingTableEntry routing_table_self i self_hash longest_prefix

                        // find the small yet closest 4 nodes for leafset
                        for i in 0..3 do
                            leafset.[i] <- findClosestSmallLeaf neighbor_leaf_set self_hash
                            Console.WriteLine("clostest small = {0}", leafset.[i])
                            neighbor_leaf_set <- neighbor_leaf_set |> Array.filter ((<>) leafset.[i] )

                        // find the big yet closest 4 nodes for leafset
                        for i in 4..7 do
                            leafset.[i] <- findClosestBigLeaf neighbor_leaf_set self_hash
                            Console.WriteLine("clostest big = {0}", leafset.[i])
                            neighbor_leaf_set <- neighbor_leaf_set |> Array.filter ((<>) leafset.[i] )

                        let localtimer = System.Diagnostics.Stopwatch()
                        //localtimer.Start()
                        //printfn "Pastry Init Small %d fin" idx
                        Console.WriteLine("{0} with hash {1} init fin", idx, self_hash)
                        Console.WriteLine("For idx={0} ", idx)
                        printfn "%A" routing_table_self
                        printfn "%A" leafset

                        alive_nodes.[idx] <- self_hash
                        alive_counter <- alive_counter + 1
                        let sender = mailbox.Sender()
                        sender <! "Pastry Init Small fin"
                        return! loop idx self_hash leafset routing_table_self neighbor_leaf_set (float(localtimer.Elapsed.TotalMilliseconds)) localtimer

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
                        small_network_nodes <- Array.zeroCreate (num_nodes/5)
                        let n_bits = int(ceil(Math.Log(float(num_nodes), 8.0)))
                        //printfn "alive has length = %d and n_bits = %d and num_nodes = %d" alive_hash.Length n_bits num_nodes
                        //printfn "7"

                        for i in 0..num_nodes/5-1 do
                            let mutable random_num = random.Next(0, alive_hash.Length)
                            //printfn "%d" random_num
                            alive_hash <- alive_hash |> Array.filter ((<>) random_num )
                            let bin_str = Convert.ToString(random_num, 8).PadLeft(n_bits, '0')
                            let mutable worker_slave_actor = spawn mailbox (sprintf "workerActor%i" i) myPastryActor
                            slave_actor_refs.[i] <- worker_slave_actor
                            slave_actor_keys <- slave_actor_keys.Add(bin_str, slave_actor_refs.[i])
                            small_network_nodes.[i] <- bin_str
                        
                        //printfn "Done8"

                        //printfn "%A" small_network_nodes
                        //printfn "%A" slave_actor_keys


                        for i in 0..1 do
                            //slave_actor_refs.[i] <! PastryInitSmall(i)
                            let mutable res = slave_actor_refs.[i] <? PastryInitSmall(i)
                            let mutable sync = Async.RunSynchronously(res, 100) |> string
                            0|>ignore

                        //timer.Start()

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