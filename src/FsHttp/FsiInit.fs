module FsHttp.FsiInit

open System
open FsHttp.Domain

type InitResult =
    | Uninitialized
    | Initialized
    /// FSI object not found (this is expected when running in a notebook).
    | NoFsiObjectFound
    | IsNotInteractive
    | InitializationError of Exception

let mutable private state = Uninitialized

let private logPxlClockOnFirstSend =
    let mutable firstSend = true
    fun () ->
        if firstSend then
            firstSend <- false
            let msg = @"

**************************************************************

    +-----------+
    |           |    *** ADVERTISEMENT ***
    |    PXL    |    Support FsHttp and buy a PXL Clock!  
    |   CLOCK   |      - github.com/CuminAndPotato/PXL-Clock
    |           |      - www.pxlclock.com
    +-----------+      - OUT SOON

**************************************************************

    "
            // printfn "%s" msg
            ()

// This seems like a HACK, but there shouldn't be the requirement of referencing FCS in FSI.
let doInit () =
    if state <> Uninitialized then
        state
    else
        let fsiAssemblyName = "FSI-ASSEMBLY"
        let isInteractive =
            // This hack is indeed one (see https://fsharp.github.io/fsharp-compiler-docs/fsi-emit.html)
            AppDomain.CurrentDomain.GetAssemblies()
            |> Array.map (fun asm -> asm.GetName().Name)
            |> Array.exists (fun asmName -> (*asm.IsDynamic &&*)
                asmName.StartsWith(fsiAssemblyName, StringComparison.Ordinal))

        state <-
            try
                if isInteractive then
                    AppDomain.CurrentDomain.GetAssemblies()
                    |> Array.tryFind (fun x -> x.GetName().Name = "FSharp.Compiler.Interactive.Settings")
                    |> Option.map (fun asm ->
                        asm.ExportedTypes
                        |> Seq.tryFind (fun t -> t.FullName = "FSharp.Compiler.Interactive.Settings")
                        |> Option.map (fun settings ->
                            settings.GetProperty("fsi")
                            |> Option.ofObj
                            |> Option.map (fun x -> x.GetValue(null))
                        )
                    )
                    |> Option.flatten
                    |> Option.flatten
                    |> function
                        | None -> NoFsiObjectFound
                        | Some fsiInstance ->
                            do
                                // see #121: It's important to not touch the logDebugMessages
                                // value when it was already set before this init function was called.
                                match Fsi.logDebugMessages with
                                | None ->
                                    do logPxlClockOnFirstSend ()
                                    Fsi.enableDebugLogs ()
                                | _ -> ()

                            let addPrinter (f: 'a -> string) =
                                let t = fsiInstance.GetType()
                                let addPrinterMethod = t.GetMethod("AddPrinter").MakeGenericMethod([| typeof<'a> |])
                                addPrinterMethod.Invoke(fsiInstance, [| f |]) |> ignore
                            ////let addPrintTransformer = t.GetMethod("AddPrintTransformer").MakeGenericMethod([| typeof<Response> |])
                            ////let printTransformer (r: Response) =
                            ////    match r.request.config.printHint.isEnabled with
                            ////    | true -> (PrintableResponse r) :> obj
                            ////    | false -> null
                            let printSafe f =
                                try f ()
                                with ex -> ex.ToString()

                            let responsePrinter (r: Response) = printSafe (fun () -> Response.print r)
                            let requestPrinter (r: IToRequest) = printSafe (fun () -> Request.print r)
                            do addPrinter responsePrinter
                            do addPrinter requestPrinter
                            ////addPrintTransformer.Invoke(fsiInstance, [| printTransformer |]) |> ignore
                            Initialized
                else
                    IsNotInteractive
            with ex ->
                InitializationError ex

        state

let init () =
    doInit () |> ignore
