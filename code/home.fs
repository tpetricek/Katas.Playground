module Katas.Server.Home

// ------------------------------------------------------------------------------------------------
// 
// ------------------------------------------------------------------------------------------------

open System
open System.IO
open Suave
open Suave.Web
open Suave.Http
open Suave.Http.Applicatives
open Suave.Types
open FSharp.Markdown
open FSharp.Literate
open Katas.Server.Common  

type Kata = 
  { Url : string 
    Heading : string 
    Description : string }

let getKataInfo file = 
  let doc = Markdown.Parse(File.ReadAllText file)
  let heading = 
    doc.Paragraphs |> Seq.tryPick (function
      | Heading(1, spans) -> Some(asPlainText spans)
      | _ -> None)

  let body = 
    doc.Paragraphs 
    |> Seq.takeWhile (function
        HorizontalRule _ -> false | _ -> true)
    |> Seq.filter (function
        Paragraph _ -> true | _ -> false)
    |> List.ofSeq

  { Url = Path.GetFileNameWithoutExtension(file) 
    Heading = defaultArg heading "Unnamed Kata" 
    Description = Markdown.WriteHtml(MarkdownDocument(body, doc.DefinedLinks)) }

let webPart = 
  path "/" >>= request (fun _ -> 
    Directory.GetFiles(__SOURCE_DIRECTORY__ + "/../katas")
    |> Seq.map getKataInfo
    |> DotLiquid.page "home.html")
