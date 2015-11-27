module Katas.Server.Common

// ------------------------------------------------------------------------------------------------
// Agent that protects a specified resource
// ------------------------------------------------------------------------------------------------

/// Agent that allows only one caller to use the specified resource.
/// This re-creates the resource after specified number of uses
/// and it calls `cleanup` on it before abandoning it.
type ResourceAgent<'T>(name, restartAfter, ctor:unit -> 'T, ?cleanup) =
  do printfn "[ResourceAgent] Initializing: %s" name
  let mutable resource = ctor()
  do printfn "[ResourceAgent] Created: %s" name
  let agent = MailboxProcessor.Start(fun inbox -> async {
    while true do
      try
        for i in 1 .. restartAfter do
          let! work = inbox.Receive()
          do! work resource
      with e ->
        printfn "[ResourceAgent] Unhandled: %A" e
      do printfn "[ResourceAgent] Cleaning and recreating: %s" name
      try cleanup |> Option.iter (fun clean -> clean resource) with _ -> ()
      resource <- ctor()
      do printfn "[ResourceAgent] Recreated: %s" name
  })
  member x.Process<'R>(work) : Async<'R> =
    agent.PostAndAsyncReply(fun reply checker -> async {
      let! res = work checker
      reply.Reply(res) })

/// Split the input string into an array of lines (using \r\n or \n as separator)
let getLines (s:string) = s.Replace("\r\n", "\n").Split('\n')

let (|Trim|) (s:string) = s.Trim().ToLower()

let replace (replacements:seq<string * string>) input = 
  replacements |> Seq.fold (fun (input:string) (k,v) -> input.Replace(k, v)) input

let (|As|) v x = v, x

open System
open System.IO
open Suave.Http
open Suave.Types

type TempFile(file, delete) =
  member x.FileName = file
  interface IDisposable with  
    member x.Dispose() =
      () //delete |> Seq.iter File.Delete
  static member Create() =
    let fn = Path.GetTempFileName()
    new TempFile(fn, [fn])
  static member Create(ext) =
    let fn = Path.GetTempFileName()
    new TempFile(fn + "." + ext, [fn; fn + "." + ext])

/// Return success and disable all caches
let noCacheSuccess res =
  Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
  >>= Writers.setHeader "Pragma" "no-cache"
  >>= Writers.setHeader "Expires" "0"
  >>= Successful.OK(res)

/// Get parameters of an F# compiler service request. Returns
/// a tuple with line, column & source (the first two may be optional)
let getRequestParams (ctx:HttpContext) =
  use sr = new StreamReader(new MemoryStream(ctx.request.rawForm))
  let toOption = function Choice1Of2 s -> Some s | _ -> None
  let tryAsInt s = match Int32.TryParse s with true, n -> Some n | _ -> None
  ctx.request.queryParam "line" |> toOption |> Option.bind tryAsInt,
  ctx.request.queryParam "col" |> toOption |> Option.bind tryAsInt,
  sr.ReadToEnd()

let withRequestParams f ctx = async {
  return! f (getRequestParams ctx) ctx }

/// Format MarkdownSpans as plain text dropping basic formatting
open FSharp.Markdown

let rec asPlainText items : string =
  items |> List.map (function
    | Literal text -> text.Replace('\n', ' ').Replace('\r', ' ')
    | DirectLink(items, _) 
    | Emphasis(items)
    | Strong(items) -> (asPlainText items).Replace('\n', ' ').Replace('\r', ' ')
    | _ -> "")
  |> String.concat " "

module List = 
  let partitionBy f items = 
    let rec loop group acc items =
      match items with
      | [] -> (List.rev group)::acc |> List.rev
      | x::xs when f x -> loop [] ((List.rev group)::acc) xs
      | x::xs -> loop (x::group) acc xs
    loop [] [] items
  
  let tryRemoveFirst f items = 
    let rec loop acc items = 
      match items with 
      | x::xs when f x -> Some x, List.rev acc @ xs
      | x::xs -> loop (x::acc) xs
      | [] -> None, List.rev acc
    loop [] items

module Seq = 
  let tryLast input =
    input |> Seq.fold (fun _ v -> Some v) None        