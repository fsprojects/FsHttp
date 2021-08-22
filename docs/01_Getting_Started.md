
## Quick Start: Build up a GET request


{% highlight fsharp %}

http {
    GET "https://reqres.in/api/users"
}
{% endhighlight %}

add a header...

{% highlight fsharp %}
http {
    GET "https://reqres.in/api/users"
    CacheControl "no-cache"
}
{% endhighlight %}

Here is an example of a POST with JSON as body:

{% highlight fsharp %}
http {
    POST "https://reqres.in/api/users"
    // after the HTTP verb, specify header properties
    CacheControl "no-cache"
    // use "body" keyword to start specifying body properties
    body
    json """
    {
        "name": "morpheus",
        "job": "leader"
    }
    """
}

// TODO: Link to API Doc
{% endhighlight %}

## Verb-First Requests (Syntax)

Alternatively, you can write the verb first.
Note that computation expressions must not be empty, so you
have to write at lease something, like 'id', 'go', 'exp', etc.

Have a look at: ```./src/FsHttp/DslCE.fs, module Shortcuts```

{% highlight fsharp %}

get "https://reqres.in/api/users" { send }
{% endhighlight %}

Inside the ```{ }```, you can place headers as usual...

{% highlight fsharp %}
get "https://reqres.in/api/users" {
    CacheControl "no-cache"
    exp
}
{% endhighlight %}

## URL Formatting (Line Breaks and Comments)

You can split URL query parameters or comment lines out by using F# line-comment syntax.
Line breaks and trailing or leading spaces will be removed:

{% highlight fsharp %}
get "https://reqres.in/api/users
            ?page=2
            //&skip=5
            &delay=3" {
    send }
{% endhighlight %}

## Response Content Transformations

There are several ways transforming the content of the returned response to
something like text or JSON:

See also: ```./src/FsHttp/ResponseHandling.fs```

{% highlight fsharp %}

http {
    POST "https://reqres.in/api/users"
    CacheControl "no-cache"
    body
    json """
    {
        "name": "morpheus",
        "job": "leader"
    }
    """
}
|> Response.toJson
{% endhighlight %}

Works of course also like this:

{% highlight fsharp %}
post "https://reqres.in/api/users" {
    CacheControl "no-cache"
    body
    json """
    {
        "name": "morpheus",
        "job": "leader"
    }
    """
    send
}
|> Response.toJson
{% endhighlight %}

Use FSharp.Data.JsonExtensions to do JSON processing:

{% highlight fsharp %}
open FSharp.Data
open FSharp.Data.JsonExtensions

http {
    GET @"https://reqres.in/api/users?page=2&delay=3"
}
|> Response.toJson
|> fun json -> json?page.AsInteger()
{% endhighlight %}

## Configuration: Timeouts, etc.

You can specify a timeout:

{% highlight fsharp %}
// should throw because it's very short
http {
    GET "http://www.google.de"
    timeoutInSeconds 0.1
}
{% endhighlight %}

You can also set config values globally (inherited when requests are created):

{% highlight fsharp %}
FsHttp.Config.setDefaultConfig (fun config ->
    { config with timeout = System.TimeSpan.FromSeconds 15.0 })
{% endhighlight %}

## Access HttpClient and HttpMessage

Transform underlying http client and do whatever you feel you gave to do:

{% highlight fsharp %}
http {
    GET @"https://reqres.in/api/users?page=2&delay=3"
    transformHttpClient (fun httpClient ->
        // this will cause a timeout exception
        httpClient.Timeout <- System.TimeSpan.FromMilliseconds 1.0
        httpClient)
}
{% endhighlight %}

Transform underlying http request message:

{% highlight fsharp %}
http {
    GET @"https://reqres.in/api/users?page=2&delay=3"
    transformHttpRequestMessage (fun msg ->
        printfn "HTTP message: %A" msg
        msg)
}
{% endhighlight %}

## Lazy Evaluation / Chaining Builders

*Hint:* Have a look at: ```./src/FsHttp/DslCE.fs, module Fsi'```

There is not only the immediate + synchronous way of specifying requests. It's also possible to
simply build a request, pass it around and send it later or to warp it in async.

Chaining builders together: First, use a httpLazy to create a 'HeaderContext'

*Hint:* ```httpLazy { ... }``` is just a shortcut for ```httpRequest StartingContext { ... }```

{% highlight fsharp %}
let postOnly =
    httpLazy {
        POST "https://reqres.in/api/users"
    }
{% endhighlight %}

Add some HTTP headers to the context:

{% highlight fsharp %}
let postWithCacheControlBut =
    postOnly {
        CacheControl "no-cache"
    }
{% endhighlight %}

Transform the HeaderContext to a BodyContext and add JSON content:

{% highlight fsharp %}
let finalPostWithBody =
    postWithCacheControlBut {
        body
        json """
        {
            "name": "morpheus",
            "job": "leader"
        }
        """
    }
{% endhighlight %}

Finally, send the request (sync or async):

{% highlight fsharp %}
let finalPostResponse = finalPostWithBody |> Request.send
let finalPostResponseAsync = finalPostWithBody |> Request.sendAsync
{% endhighlight %}

### Async Builder

HTTP in an async context:

{% highlight fsharp %}
let pageAsync =
    async {
        let! response = 
            httpAsync {
                GET "https://reqres.in/api/users?page=2&delay=3"
            }
        let page =
            response
            |> Response.toJson
            |> fun json -> json?page.AsInteger()
        return page
    }


// TODO Document naming conventions according to: https://github.com/ronaldschlenker/FsHttp/issues/48
{% endhighlight %}

## Naming Conventions

*Names for naming conventions according to: https://en.wikipedia.org/wiki/Naming_convention_(programming)#Lisp*

* Naming of **HTTP methods inside of a builder** are **upper flat case** (following https://tools.ietf.org/html/rfc7231#section-4).
    
    *Example:*
    ```fsharp
    http {
        GET "http://www.whatever.com"
    }
    ```

* Naming of **HTTP methods used outside of a builder** follow the F# naming convention and are **flat case**.

    *Example:*
    ```fsharp
    let request = get "http://www.whatever.com"
    ```

* Naming of **HTTP headers inside of a builder** are **PascalCase**. Even though they should be named **train case** (according to https://tools.ietf.org/html/rfc7231#section-5), it would require a double backtic using it in F#, which might be uncomfortable.

    *Example:*
    ```fsharp
    http {
        // ...
        CacheControl "no-cache"
    }
    ```

* Naming of **all other constructs** are **lower camel case**. This applies to:
    * config methods
    * type transformer (like "body")
    * content annotations (like "json" or "text")
    * FSI print modifiers like "expand" or "preview"
    * invocations like "send"

    *Example:*
    ```fsharp
    http {
        // ...
        timeoutInSeconds 10.0
        body
        json """ { ... } """
        expand
    }
    ```

## Examples for building, chaining and sending requests


{% highlight fsharp %}

let getUsers1 : LazyHttpBuilder<HeaderContext> = get "https://reqres.in/api/users"
let getUsers2 : LazyHttpBuilder<HeaderContext> = httpLazy { GET "https://reqres.in/api/users" }
let _ : Response = getUsers1 { send }
let _ : Response = get "https://reqres.in/api/users" { send }
let _ : Response = getUsers1 |> Request.send
let _ : Response = http { GET "https://reqres.in/api/users" }
let _ : Async<Response> = httpAsync { GET "https://reqres.in/api/users" }
let _ : Response =
    httpLazy {
        GET "https://reqres.in/api/users"
        send
    }
let _ : Async<Response> =
    httpLazy {
        GET "https://reqres.in/api/users"
        sendAsync
    }

// FSI
let _ : Response =
    http {
        GET "https://reqres.in/api/users"
        CacheControl "no-cache"
        exp
    }

let _ : Response =
    get "https://reqres.in/api/users" {
        CacheControl "no-cache"
        exp
        send
    }