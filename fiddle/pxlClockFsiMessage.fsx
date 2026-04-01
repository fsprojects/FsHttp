
#r "../src/FsHttp/bin/debug/net6.0/FsHttp.dll"

open FsHttp
open FsHttp.Operators


http {
    GET "https://www.wikipedia.de"
}
|> Request.send
|> ignore
