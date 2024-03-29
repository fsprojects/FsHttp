﻿module FsHttp.Tests.``Urls and Query``

open FsUnit
open FsHttp
open FsHttp.Tests.TestHelper
open FsHttp.Tests.Server

open NUnit.Framework

open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful


[<TestCase>]
let ``Multiline urls`` () =
    use server =
        GET >=> request (fun r -> (query "q1" r) + "_" + (query "q2" r) |> OK) |> serve

    http {
        GET(
            url
                @"
                    ?q1=Query1
                    &q2=Query2"
        )
    }
    |> Request.send
    |> Response.toText
    |> should equal "Query1_Query2"


[<TestCase>]
let ``Comments in urls are discarded`` () =
    use server =
        GET
        >=> request (fun r -> (query "q1" r) + "_" + (query "q2" r) + "_" + (query "q3" r) |> OK)
        |> serve

    http {
        GET(
            url
                @"
                    ?q1=Query1
                    //&q2=Query2
                    &q3=Query3"
        )
    }
    |> Request.send
    |> Response.toText
    |> should equal ("Query1_" + keyNotFoundString + "_Query3")


[<TestCase>]
let ``Empty query params`` () =
    use server = GET >=> request (fun _ -> "" |> OK) |> serve

    http {
        GET(url "")
        query []
    }
    |> Request.send
    |> Response.toText
    |> should equal ""


[<TestCase>]
let ``Merge query params with url params`` () =
    use server =
        GET >=> request (fun r -> (query "q1" r) + "_" + (query "q2" r) |> OK) |> serve

    http {
        GET(url "?q1=Query1")
        query [ "q2", "Query2" ]
    }
    |> Request.send
    |> Response.toText
    |> should equal "Query1_Query2"


[<TestCase>]
let ``Query params`` () =
    use server =
        GET >=> request (fun r -> (query "q1" r) + "_" + (query "q2" r) |> OK) |> serve

    http {
        GET(url "")
        query [ "q1", "Query1"; "q2", "Query2" ]
    }
    |> Request.send
    |> Response.toText
    |> should equal "Query1_Query2"


[<TestCase>]
let ``Query params encoding`` () =
    use server = GET >=> request (fun r -> query "q1" r |> OK) |> serve

    http {
        GET(url "")
        query [ "q1", "<>" ]
    }
    |> Request.send
    |> Response.toText
    |> should equal "<>"
