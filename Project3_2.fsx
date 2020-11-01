#load "bootstrap.fsx"

open System
open Akka.FSharp
open Akka.Configuration

let L = 16
let b = 3

let mutable nodeRefs = Array.empty
let mutable nodesAlive = Array.empty
let mutable messageDeliveryCounter = -1
let numOfSignificantDisgits = 8

let system = System.create "system" <| ConfigurationFactory.Default()

type NodeMessage =
| Initialize of int*int     //idx of current node, index of known neighbour
| LetMeJoin of int*int      //Destination address (which is address of node joining the network), idx of node trying to join network
| LetXJoin of int*int*array<array<int>>*int //destID, idx of X, routing table for X, Id of prev node in routing
| TakeMyNeighbourSet of array<int>
| TakeMyLeafSet of array<int>
// | YouAreIn of array<int>*array<array<int>>*array<int>        //leafset, routing table, neighbourhoodset
// | TakeMyState of array<int>*array<array<int>>*array<int>     //leafset, routing table, neighbourhoodset


let getClosestLiveNode node =
    let mutable i = 1
    if not (Array.contains true nodesAlive) then
        -1
    else
        while ((node-i)>=0) && ((node+i)<(Array.length nodeRefs)) && (not (Array.get nodesAlive (node-i))) && (not (Array.get nodesAlive (node+i))) do
            i <- i+1
        while ((node-i)<0) && (not (Array.get nodesAlive (node+i))) do
            i <- i+1
        while ((node+i)>=(Array.length nodeRefs)) && (not (Array.get nodesAlive (node-i))) do
            i <- i+1
        let mutable x = -1
        if (node-i) < 0 then
            x <- node+i
        elif (node+i) >= (Array.length nodeRefs) then
            x <- node-i
        elif (Array.get nodesAlive (node-i)) then
            x <- node-i
        elif (Array.get nodesAlive (node+i)) then
            x <- node+i
        x

let getID() =
    -1

let nodeActorFunction(mailbox: Actor<_>) =

    let rec loop(ID, leafSet, routingTable, neighbourhoodSet) = actor {
        let! msg = mailbox.Receive()
        match msg with
        | Initialize (myIdx, initiallyKnownNode) ->
            if initiallyKnownNode = -1 then
                let newID = getID()
                // let newRoutingTable = Array.create ((2.0 ** (b |> double)) |> int) (Array.create numOfSignificantDisgits -1)
                let newRoutingTable = Array.create numOfSignificantDisgits (Array.create ((2.0 ** (b |> double)) |> int) -1)
                let newNeighbourhoodSet = Array.create L -1
                let newLeafSet = Array.create L -1
                nodesAlive.[myIdx] <- true
                return! loop(newID, newLeafSet, newRoutingTable, newNeighbourhoodSet)
            if not (nodesAlive.[initiallyKnownNode]) then
                Console.WriteLine("Node provided for initialisation is not alive")
                exit(-1)
            let newID = getID()
            nodeRefs.[initiallyKnownNode] <! LetMeJoin(newID, myIdx) //dont mark this as alive until it reaches the final node
            return! loop(newID, leafSet, routingTable, neighbourhoodSet)
        | LetMeJoin(destID, newNodeIdx) ->
            // check if current node itself is the destination. if yes send back all 3 state variables to X. else do below
            let rowIdx = getRowIdx(destID, routingTable)
            let mutable newRoutingTable = Array.create numOfSignificantDisgits (Array.create ((2.0 ** (b |> double)) |> int) -1)
            for i = 0 to rowIdx do
                for j = 0 to ((2.0 ** (b |> double)) |> int) do
                    if newRoutingTable.[i].[j] = -1 then
                        newRoutingTable.[i].[j] <- routingTable.[i].[j]
            //CONTINUE HERE
            nodeRefs.[newNodeIdx] <! TakeMyNeighbourSet(neighbourhoodSet)
            let nextHop = getNextHop(destID, routingTable)
            if nextHop <> -1 then
                nodeRefs.[nextHop] <! LetXJoin(destID, newNodeIdx, newRoutingTable, ID)

    }
    loop(-1, [||], [||], [||])

let main =
    let numOfNodes = 10
    let numOfMessages = 10
    nodeRefs <- Array.zeroCreate numOfNodes
    nodesAlive <- Array.create numOfNodes false
    messageDeliveryCounter <- numOfMessages

    for i = 0 to numOfNodes-1 do
        Array.set nodeRefs i (spawn system (sprintf "nodeActor%i" i) nodeActorFunction)
    
    let initNodeZero = (Array.get nodeRefs 0) <? Initialize(0, -1)
    let sync = Async.RunSynchronously(initNodeZero, 1000) |> string

    for i = 1 to numOfNodes-1 do


    while messageDeliveryCounter >= 0 do
        0 |> ignore