module Katas.Server.Document

// ------------------------------------------------------------------------------------------------
// 
// ------------------------------------------------------------------------------------------------

open System
open System.IO
open Suave
open Suave.Web
open Suave.Http
open Suave.Types
open Katas.Server.Common
open FSharp.Markdown
open FSharp.Literate

let invalidChars = set(Path.GetInvalidFileNameChars())
let editorTemplatePath = Path.Combine(__SOURCE_DIRECTORY__, "..", "templates", "kata-editor.html")
let testTemplatePath = Path.Combine(__SOURCE_DIRECTORY__, "..", "templates", "kata-test.html")
 
type Section =
  { ID : string
    Title : string
    Body : string }

type Document = 
  { Introduction : string 
    Sections : seq<Section> }


let agent = FSharp.CodeFormat.CodeFormat.CreateAgent()

let transformBlock section counter par =
  let elementId = "output_" + (string counter)
  let sectionId = "section_" + (string section)
  match par with 
  | Matching.LiterateParagraph(LanguageTaggedCode(Trim "source", code)) ->
      let editorTemplate = File.ReadAllText(editorTemplatePath)
      let encoded = System.Web.HttpUtility.JavaScriptStringEncode(code.Trim())
      editorTemplate
      |> replace [ "[ID]", elementId; "[SECTION]", sectionId; "[SOURCE]", encoded ]
      |> InlineBlock

  | Matching.LiterateParagraph(LanguageTaggedCode(Trim "test", code)) ->
      let testTemplate = File.ReadAllText(testTemplatePath)
      let encoded = System.Web.HttpUtility.JavaScriptStringEncode(code.Trim())
      let snip, _ = FSharp.CodeFormat.CodeFormat.CreateAgent().ParseSource("/Script.fsx", code)
      let fmt = FSharp.CodeFormat.CodeFormat.FormatHtml(snip, "fs"+(string counter), false, false)
      let html = (String.concat "\n" [ for s in fmt.Snippets -> s.Content ]) + fmt.ToolTip

      testTemplate
      |> replace [ "[ID]", elementId; "[SECTION]", sectionId; "[SOURCE]", encoded; "[HTML]", html ]
      |>  InlineBlock

  | _ -> par

let rec transformBlocks section counter acc pars = 
  match pars with
  | [] -> List.rev acc
  | (HorizontalRule c as par)::pars ->
      transformBlocks (section+1) (counter+1) (par::acc) pars
  | par::pars -> 
      let par = transformBlock section counter par
      transformBlocks section (counter+1) (par::acc) pars

let transform path = async {
  let docOrig = Literate.ParseMarkdownString(File.ReadAllText(path))
  let docTrans = docOrig.With(transformBlocks 0 0 [] docOrig.Paragraphs)
  let doc = Literate.FormatLiterateNodes(docTrans, OutputKind.Html, lineNumbers=false, generateAnchors=true)

  let sections = doc.Paragraphs |> List.partitionBy (function HorizontalRule _ -> true | _ -> false)

  let formatParagraphs newPars =
    let newDoc = MarkdownDocument(newPars, doc.DefinedLinks)
    Markdown.WriteHtml(newDoc)

  let first, rest =
    match sections with
    | [] -> failwith "Empty document!"
    | first::rest -> first, rest

  let sections =
    rest |> List.mapi (fun i pars ->
      let heading, pars = 
        pars |> List.tryRemoveFirst (function Heading _ -> true | _ -> false)
      let title = heading |> Option.map (function Heading(_, items) -> asPlainText items | _ -> "")
      { ID = sprintf "sec_%d" i 
        Title = defaultArg title (sprintf "Section %d" i)
        Body = formatParagraphs pars })
  return 
    { Introduction = formatParagraphs first 
      Sections = sections } }

let renderMarkdown md =
  Markdown.TransformHtml(md)

let webPart ctx = async {
  let file = ctx.request.url.LocalPath
  if file.[0] <> '/' || (Seq.exists invalidChars.Contains file.[1 ..]) then return None else
  let path = Path.Combine(__SOURCE_DIRECTORY__, "..", "katas", file.Substring(1) + ".md")
  if File.Exists(path) then
    let! page = transform path
    return! ctx |> DotLiquid.page "kata.html" page
  else return None }
