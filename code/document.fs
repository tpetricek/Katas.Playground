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
open FSharp.Markdown
open FSharp.Literate

let invalidChars = set(Path.GetInvalidFileNameChars())
let pageTemplatePath = Path.Combine(__SOURCE_DIRECTORY__, "..", "templates", "page.html")
let editorTemplatePath = Path.Combine(__SOURCE_DIRECTORY__, "..", "templates", "editor.html")
let testTemplatePath = Path.Combine(__SOURCE_DIRECTORY__, "..", "templates", "test.html")
 
let (|Trim|) (s:string) = s.Trim().ToLower()

let agent = FSharp.CodeFormat.CodeFormat.CreateAgent()

let transformBlock counter (par:MarkdownParagraph) = 
  let id = "output_" + (string counter)
  match par with 
  | CodeBlock(code, Trim "fsharp", Trim "source") ->
      let editorTemplate = File.ReadAllText(editorTemplatePath)
      let encoded = System.Web.HttpUtility.JavaScriptStringEncode(code)
      InlineBlock(editorTemplate.Replace("[ID]", id).Replace("[SOURCE]", encoded)) 
  | CodeBlock(code, Trim "fsharp", Trim "test") ->     
      let editorTemplate = File.ReadAllText(testTemplatePath)
      let encoded = System.Web.HttpUtility.JavaScriptStringEncode(code)

      let snip, err = FSharp.CodeFormat.CodeFormat.CreateAgent().ParseSource("/Script.fsx", code)
      let fmt = FSharp.CodeFormat.CodeFormat.FormatHtml(snip, "fs"+(string counter))
      let html = (String.concat "\n" [ for s in fmt.Snippets -> s.Content ]) + fmt.ToolTip

      InlineBlock(editorTemplate.Replace("[ID]", id).Replace("[SOURCE]", encoded).Replace("[HTML]", html)) 
  | par -> par

let transform fsi template path = async {
  let pageTemplate = File.ReadAllText(template)
  let doc = Markdown.Parse(File.ReadAllText(path))
  let newDoc = MarkdownDocument(doc.Paragraphs |> List.mapi transformBlock, doc.DefinedLinks)
  let html = Markdown.WriteHtml(newDoc)
  return pageTemplate.Replace("[BODY]", html) }

let renderMarkdown md =
  Markdown.TransformHtml(md)

let renderDocument fsi ctx = async {
  let file = ctx.request.url.LocalPath
  if file.[0] <> '/' || (Seq.exists invalidChars.Contains file.[1 ..]) then return None else
  let path = Path.Combine(__SOURCE_DIRECTORY__, "..", "katas", file.Substring(1) + ".md")
  if File.Exists(path) then
    let! html = transform fsi pageTemplatePath path
    return! ctx |> Successful.OK(html)
  else return None }

open Suave.Http.Applicatives

let webPart fsi = renderDocument fsi