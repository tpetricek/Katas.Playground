#r "packages/Suave/lib/net40/Suave.dll"
open Suave
open Suave.Http

#r "System.Web.dll"
#r "packages/FSharp.Compiler.Service/lib/net40/FSharp.Compiler.Service.dll"
#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"

#load "code/config.fs"
#load "code/common.fs"
#load "code/evaluator.fs"
#load "code/document.fs"
#load "code/editor.fs"

open System
open System.IO
open Suave.Types
open Suave.Http.Applicatives
open Katas.Server
open Katas.Server.Common
open Microsoft.FSharp.Compiler.SourceCodeServices

let staticWebFile ctx = async {
  let local = ctx.request.url.LocalPath
  let file = if local = "/" then "index.html" else local.Substring(1)
  let actualFile = 
    [ Path.Combine(ctx.runtime.homeDirectory, "web", file)
      Path.Combine(ctx.runtime.homeDirectory, "katas", file) ]
    |> Seq.tryFind File.Exists
  printfn "File: %A" actualFile
  match actualFile with
  | None -> return None
  | Some actualFile ->
      let mime = Suave.Http.Writers.defaultMimeTypesMap(Path.GetExtension(actualFile))
      let setMime =
        match mime, Path.GetExtension(actualFile).ToLower() with
        | Some mime, _ -> Suave.Http.Writers.setMimeType mime.name
        | _, ".pdf" -> Suave.Http.Writers.setMimeType "application/pdf"
        | _ -> fun c -> async { return None }
      return! ctx |> ( setMime >>= Successful.ok(File.ReadAllBytes actualFile)) }

let checker =
  ResourceAgent("FSharpChecker", Int32.MaxValue, fun () -> FSharpChecker.Create())

let fsi =
  ResourceAgent("FsiSession", 50,
    (fun () -> Evaluator.startSession Config.gammaFolder Config.loadScriptString),
    (fun fsi -> (fsi.Session :> IDisposable).Dispose()) )

let app =
  choose
    [ Editor.webPart checker
      Document.webPart fsi
      Evaluator.webPart fsi
      staticWebFile
      RequestErrors.NOT_FOUND("Not found") ]

// -------------------------------------------------------------------------------------------------
// To run the web site, you can use `build.sh` or `build.cmd` script, which is nice because it
// automatically reloads the script when it changes. But for debugging, you can also use run or
// run with debugger in VS or XS. This runs the code below.
// -------------------------------------------------------------------------------------------------
#if INTERACTIVE
#else
open Suave.Web
let cfg =
  { defaultConfig with
      bindings = [ HttpBinding.mk' HTTP  "127.0.0.1" 8011 ]
      homeFolder = Some __SOURCE_DIRECTORY__ }
let _, server = startWebServerAsync cfg app
Async.Start(server)
System.Diagnostics.Process.Start("http://localhost:8011")
System.Console.ReadLine() |> ignore
#endif