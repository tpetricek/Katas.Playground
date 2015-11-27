module Katas.Server.Evaluator

open System
open System.IO
open System.Text
open Katas.Server.Common
open Microsoft.FSharp.Compiler.SimpleSourceCodeServices

// ------------------------------------------------------------------------------------------------
// FunScript + F# Compiler Service Evaluator
// ------------------------------------------------------------------------------------------------

type CompilerSession =
  { Compiler : SimpleSourceCodeServices 
    AppDomain : AppDomain }

let startSession () =
  { Compiler = SimpleSourceCodeServices()
    AppDomain = 
      System.AppDomain.CreateDomain
        ("Evaluator-"+System.Guid.NewGuid().ToString(), Security.Policy.Evidence(), 
          AppDomainSetup(ApplicationBase = __SOURCE_DIRECTORY__ + "/../lib")) }  

/// Pass the specified code to FunScript and return JavaScript that we'll
/// send back to the client (so that they can run it themselves)
let evalFunScript code { AppDomain = appDomain; Compiler = scs } = async {
  let allCode =
    [ yield "[<ReflectedDefinition>]"
      yield "module Main"
      for line in getLines code do yield "  " + line ]

  use source = TempFile.Create("fs")
  use library = TempFile.Create("dll")
  File.WriteAllLines(source.FileName, allCode)
  let errors, exitCode = scs.Compile([| "fsc.exe"; "-o"; library.FileName; "-a"; source.FileName |])
  printfn "Compiling:\n%A\nInto: %s" allCode library.FileName
  if exitCode <> 0 then
    return Choice2Of2(new Exception(sprintf "Evaluation failed: %A" errors))
  else
    let ts = Katas.Server.Translator(File.ReadAllBytes library.FileName)

    let del = Delegate.CreateDelegate(typeof<CrossAppDomainDelegate>, ts, typeof<Translator>.GetMethod("Run"))
    appDomain.DoCallBack(del :?> CrossAppDomainDelegate)
    return Choice1Of2(ts.Result) }


// ------------------------------------------------------------------------------------------------
// Start F# interactive and expose a web part
// ------------------------------------------------------------------------------------------------

open Suave.Http
open Suave.Http.Applicatives

let webPart (scs:ResourceAgent<_>) =
  path "/run" >>= withRequestParams (fun (_, _, code) ctx -> async { 
    // Transform F# `source` into JavaScript and return it
    let! jscode = scs.Process(evalFunScript code)
    match jscode with
    | Choice1Of2 jscode -> return! ctx |> noCacheSuccess jscode
    | Choice2Of2 e -> 
        printfn "Evalutaiton failed: %s" (e.ToString())
        return! ctx |> RequestErrors.BAD_REQUEST "evaluation failed" })