module FsHttp.Helper

open System
open System.IO
open System.Text
open System.Net.Http.Headers
open System.Runtime.InteropServices
open FsHttp

[<RequireQualifiedAccess>]
module Encoding =
    let base64 = Encoding.GetEncoding("ISO-8859-1")

[<RequireQualifiedAccess>]
module Async =
    let map f x =
        async {
            let! x = x
            return f x
        }

type StringBuilder with
    member sb.append(s: string) = sb.Append s |> ignore
    member sb.appendLine(s: string) = sb.AppendLine s |> ignore
    member sb.newLine() = sb.appendLine ""

    member sb.appendSection(s: string) =
        sb.appendLine s
        String('-', s.Length + 1) |> sb.appendLine

[<RequireQualifiedAccess>]
module Map =
    let union (m1: Map<'k, 'v>) (s: seq<'k * 'v>) =
        seq {
            yield! m1 |> Seq.map (fun kvp -> kvp.Key, kvp.Value)
            yield! s
        }
        |> Map.ofSeq

[<RequireQualifiedAccess>]
module Result =
    let getValueOrThrow ex (r: Result<'a, 'b>) =
        match r with
        | Ok value -> value
        | Error value -> raise (ex value)

[<RequireQualifiedAccess>]
module String =
    let urlEncode (s: string) = System.Web.HttpUtility.UrlEncode(s)
    let toBase64 (s: string) = s |> Encoding.base64.GetBytes |> Convert.ToBase64String
    let fromBase64 (s: string) = s |> Convert.FromBase64String |> Encoding.base64.GetString
    let substring maxLength (s: string) = string (s.Substring(0, Math.Min(maxLength, s.Length)))

[<RequireQualifiedAccess>]
module Url =
    let combine (url1: string) (url2: string) =
        let del = '/'
        let sdel = string del
        let norm (s: string) = s.Trim().Replace(@"\", sdel)
        let delTrim = [| del |]
        let a = (norm url1).TrimEnd(delTrim)
        let b = (norm url2).TrimStart(delTrim).TrimEnd(delTrim)
        a + sdel + b

[<RequireQualifiedAccess>]
module Stream =
    let readUtf8StringAsync maxLen (stream: Stream) =
        // we could definitely optimize this
        async {
            use reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks = false, bufferSize = 1024, leaveOpen = true)
            let sb = StringBuilder()
            let mutable codePointsRead = 0
            let buffer = Array.zeroCreate<char> 1 // Buffer to read one character at a time
            while codePointsRead < maxLen && not reader.EndOfStream do
                // Read the next character asynchronously
                let! charsRead = reader.ReadAsync(buffer, 0, 1) |> Async.AwaitTask
                if charsRead > 0 then
                    let c = buffer.[0]
                    sb.Append(c) |> ignore
                    // Check if the character is a high surrogate
                    if Char.IsHighSurrogate(c) && not reader.EndOfStream then
                        // Read the low surrogate asynchronously and append it
                        let! nextCharsRead = reader.ReadAsync(buffer, 0, 1) |> Async.AwaitTask
                        if nextCharsRead > 0 then
                            let nextC = buffer.[0]
                            sb.Append(nextC) |> ignore
                    // Increment the code point count
                    codePointsRead <- codePointsRead + 1
            return sb.ToString()
        }

    let readUtf8StringTAsync maxLength (stream: Stream) =
        readUtf8StringAsync maxLength stream |> Async.StartAsTask

    let copyToCallbackAsync (target: Stream) callback (source: Stream) =
        async {
            let buffer = Array.create 81920 (byte 0)
            let logTimeSpan = TimeSpan.FromSeconds 1.5
            let mutable continueLooping = true
            let mutable overallBytesCount = 0
            let mutable lastNotificationTime = DateTime.Now

            while continueLooping do
                let! readCount = source.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask
                do target.Write(buffer, 0, readCount)
                do overallBytesCount <- overallBytesCount + readCount
                let now = DateTime.Now

                if (now - lastNotificationTime) > logTimeSpan then
                    do callback overallBytesCount
                    do lastNotificationTime <- now

                do continueLooping <- readCount > 0

            callback overallBytesCount
        }

    let copyToCallbackTAsync (target: Stream) callback (source: Stream) =
        copyToCallbackAsync target callback source |> Async.StartAsTask

    let copyToAsync target source =
        async {
            Fsi.logfn "Download response received - starting download..."

            do!
                source
                |> copyToCallbackAsync
                    target
                    (fun read ->
                        let mbRead = float read / 1024.0 / 1024.0
                        Fsi.logfn "%f MB" mbRead
                    )

            Fsi.logfn "Download finished."
        }

    let copyToTAsync target source = copyToAsync target source |> Async.StartAsTask

    let toStringUtf8Async source =
        async {
            use ms = new MemoryStream()
            do! source |> copyToAsync ms
            do ms.Position <- 0L
            use sr = new StreamReader(ms, Encoding.UTF8)
            return sr.ReadToEnd()
        }

    let toStringUtf8TAsync source = toStringUtf8Async source |> Async.StartAsTask

    let toBytesAsync source =
        async {
            use ms = new MemoryStream()
            do! source |> copyToAsync ms
            return ms.ToArray()
        }

    let toBytesTAsync source = toBytesAsync source |> Async.StartAsTask

    let saveFileAsync fileName source =
        async {
            Fsi.logfn "Download response received (file: %s) - starting download..." fileName
            use fs = File.Open(fileName, FileMode.Create, FileAccess.Write)
            do! source |> copyToAsync fs
            Fsi.logfn "Download finished."
        }

    let saveFileTAsync fileName source = saveFileAsync fileName source |> Async.StartAsTask


type EnumerableStream(source: byte seq) =
    inherit Stream()

    let enumerator = source.GetEnumerator()
    let mutable isDisposed = false
    let mutable position = 0L

    override _.CanRead = true
    override _.CanSeek = false
    override _.CanWrite = false

    override _.Length = raise (NotSupportedException())

    override _.Position
        with get() = position
        and set(_) = raise (NotSupportedException())

    override _.Flush() = ()

    override _.Read(buffer: byte[], offset: int, count: int) =
        let bytesToRead = Math.Min(count, buffer.Length - int position)
        if bytesToRead <= 0 then 0
        else
            let mutable bytesRead = 0
            while bytesRead < bytesToRead && enumerator.MoveNext() do
                buffer.[offset + bytesRead] <- enumerator.Current
                bytesRead <- bytesRead + 1
            position <- position + int64 bytesRead
            bytesRead

    override _.Seek(_: int64, _: SeekOrigin) = raise (NotSupportedException())
    override _.SetLength(_: int64) = raise (NotSupportedException())
    override _.Write(_: byte[], _: int, _: int) = raise (NotSupportedException())

    override _.Dispose(disposing: bool) =
        if not isDisposed then
            isDisposed <- true
            enumerator.Dispose()
