#time "on"
#r @"bin/Debug/netcoreapp3.1/Akka.dll"
#r @"bin/Debug/netcoreapp3.1/Akka.FSharp.dll"

open System
open Akka.Actor
open Akka.FSharp
open System.Diagnostics

let system = ActorSystem.Create("FSharp")

let sumList list = List.fold (fun a elem -> a + elem * elem) 0 list

//printfn "Sum of the elements of list %A is %d." [ 1 .. 3 ] (sumList [ 1 .. 3 ]) 
//let sq = sumList [ 1 .. 24 ]

let perfectSquare n =
    let h = n &&& 0xF
    if (h > 9) then false
    else
        if ( h <> 2 && h <> 3 && h <> 5 && h <> 6 && h <> 7 && h <> 8 ) then
            let t = ((n |> double |> sqrt) + 0.5) |> floor|> int
            t*t = n
        else false

let myActor (mailbox: Actor<_>)= 
    let rec loop () = actor {
        let! message  =  mailbox.Receive () 
        match box message with 
        | :? string as message ->
            let input = message
            let vals : string[]=  input.Split[|' '|]
            let index = vals.[0]
            let k = vals.[1]
            //let l = [index |> int .. k |> int]
            //let sq = sumList [ index |> int .. index+k |> int]
            //let sq = sumList l    
            //let mutable response = false
            //response <- perfectSquare sq
            
            //if response = true then
              //  System.Console.WriteLine(sq |> int)
            return! loop ()
        | _ -> failwith "unknown message"
  
    }
    loop ()



let x id = "myActor" + string(id)
let echoServers = 
    [1 .. 10]
    |> List.map(fun id -> let nex_x = x id
                          spawn system nex_x myActor)
(*
for id in [0 .. 9] do
    id |> List.nth echoServers <! sprintf "F# request %d!" id
    //System.Console.WriteLine(response)
    ()
    *)

let N = 1000000 
let k = 4
let num_of_actors = 10
let mutable j = 1
while (j < (N - k)) do
    for i in [0 .. num_of_actors-1] do
        if j+i <= N - k then
            i |> List.nth echoServers <! sprintf "%d %d" (j+i) k
    j <- j + num_of_actors