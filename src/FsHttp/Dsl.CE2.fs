#if INTERACTIVE
#r "./bin/Debug/net6.0/FsHttp.dll"
#else
module FsHttp.DslCEPreview
#endif

open FsHttp

// ---------
// Builder
// ---------


type HttpBuilder() =
    member _.Delay(f: unit -> _) = f ()

    member _.Zero() = id
    
    member _.Yield(t: HeaderContext -> HeaderContext) = t
    member _.Yield(t: _ option -> HeaderContext -> HeaderContext) = t None
    member _.Yield(t: _ option -> _ option -> HeaderContext -> HeaderContext) = t None None
    
    member _.Yield(t: HeaderContext -> BodyContext) = t
    member _.Yield(t: _ option -> HeaderContext -> BodyContext) = t None
    member _.Yield(t: _ option -> _ option -> HeaderContext -> BodyContext) = t None None

    member _.Yield(t: BodyContext -> BodyContext) = t
    member _.Yield(t: _ option -> BodyContext -> BodyContext) = t None
    member _.Yield(t: _ option -> _ option -> BodyContext -> BodyContext) = t None None

    member _.Yield(t: HeaderContext -> MultipartContext) = t
    member _.Yield(t: _ option -> HeaderContext -> MultipartContext) = t None
    member _.Yield(t: _ option -> _ option -> HeaderContext -> MultipartContext) = t None None

    member _.Yield(t: MultipartContext -> MultipartContext) = t
    member _.Yield(t: _ option -> MultipartContext -> MultipartContext) = t None
    member _.Yield(t: _ option -> _ option -> MultipartContext -> MultipartContext) = t None None

    member _.Yield(t: MultipartContext -> MultipartElementContext) = t
    member _.Yield(t: _ option -> MultipartContext -> MultipartElementContext) = t None
    member _.Yield(t: _ option -> _ option -> MultipartContext -> MultipartElementContext) = t None None
    
    member _.Yield(t: MultipartElementContext -> MultipartElementContext) = t
    member _.Yield(t: _ option -> MultipartElementContext -> MultipartElementContext) = t None
    member _.Yield(t: _ option -> _ option -> MultipartElementContext -> MultipartElementContext) = t None None

    // define header after header
    member _.Combine
        (
            outer: HeaderContext -> HeaderContext, 
            inner: HeaderContext -> HeaderContext
        )        : HeaderContext -> HeaderContext 
        =
        fun hc -> inner (outer hc)

    // transition from header to body (using "body")
    member _.Combine
        (
            outer: HeaderContext -> HeaderContext,
            inner: HeaderContext -> BodyContext
        )        : HeaderContext -> BodyContext 
        =
        fun hc -> inner (outer hc)

    // transition from body (pseudo-keyword) to actual body definition
    member _.Combine
        (
            outer: HeaderContext -> BodyContext,
            inner: BodyContext -> BodyContext
        )        : HeaderContext -> BodyContext 
        =
        fun hc -> inner (outer hc)

    // define body when already defining a body above
    member _.Combine
        (
            outer: BodyContext -> BodyContext, 
            inner: BodyContext -> BodyContext
        )        : BodyContext -> BodyContext 
        =
        fun hc -> inner (outer hc)

    // transition from header to multipart (using "multipart")
    member _.Combine
        (
            outer: HeaderContext -> HeaderContext,
            inner: HeaderContext -> MultipartContext
        )        : HeaderContext -> MultipartContext 
        =
        fun hc -> inner (outer hc)

    // transition from multipart (pseudo-keyword) to actual part definition
    member _.Combine
        (
            outer: HeaderContext -> MultipartContext, 
            inner: MultipartContext -> MultipartElementContext
        )        : HeaderContext -> MultipartElementContext 
        =
        fun hc -> inner (outer hc)

    // define part when already defining a part above
    member _.Combine
        (
            outer: MultipartContext -> MultipartContext, 
            inner: MultipartContext -> MultipartContext
        )        : MultipartContext -> MultipartContext 
        =
        fun hc -> inner (outer hc)

    // back-jump from part to part
    member _.Combine
        (
            outer: HeaderContext -> HeaderContext, 
            inner: HeaderContext -> MultipartElementContext
        )        : HeaderContext -> MultipartElementContext 
        =
        fun hc -> inner (outer hc)

    // define 2 parts
    member _.Combine
        (
            outer: MultipartContext -> MultipartElementContext,
            inner: MultipartContext -> MultipartElementContext
        )        : MultipartContext -> MultipartElementContext
        =
        fun hc -> inner ((outer hc :> IToMultipartContext).ToMultipartContext())

    // define the part
    member _.Combine
        (
            outer: MultipartContext -> MultipartElementContext,
            inner: MultipartElementContext -> MultipartElementContext
        )        : MultipartContext -> MultipartElementContext
        =
        fun hc -> inner (outer hc)

    // "begin" a new part when already defining a part above
    member _.Combine
        (
            outer: MultipartElementContext -> MultipartElementContext,
            inner: MultipartContext -> MultipartElementContext
        )        : MultipartElementContext -> MultipartElementContext
        =
        fun hc -> inner ((outer hc :> IToMultipartContext).ToMultipartContext())
    

    member _.Run(t: HeaderContext -> HeaderContext) =
        t (HeaderContext.create ())
    member _.Run(t: HeaderContext -> BodyContext) =
        t (HeaderContext.create ())
    member _.Run(t: HeaderContext -> MultipartContext) =
        t (HeaderContext.create ())
    member _.Run(t: HeaderContext -> MultipartElementContext) =
        t (HeaderContext.create ())


let http = HttpBuilder()


[<AutoOpen>]
module Methods =
    let GET url ctx = HeaderContext.setUrl HttpMethods.get url ctx
    let PUT url ctx = HeaderContext.setUrl HttpMethods.put url ctx
    let POST url ctx = HeaderContext.setUrl HttpMethods.post url ctx
    let DELETE url ctx = HeaderContext.setUrl HttpMethods.delete url ctx
    let OPTIONS url ctx = HeaderContext.setUrl HttpMethods.options url ctx
    let HEAD url ctx = HeaderContext.setUrl HttpMethods.head url ctx
    let TRACE url ctx = HeaderContext.setUrl HttpMethods.trace url ctx
    let CONNECT url ctx = HeaderContext.setUrl HttpMethods.connect url ctx
    let PATCH url ctx = HeaderContext.setUrl HttpMethods.patch url ctx


[<AutoOpen>]
module Header =

    /// Append query params
    let Query queryParams (ctx: HeaderContext) =
        Header.query queryParams ctx

    /// Custom headers
    let Headers headers (ctx: HeaderContext) =
        Header.headers headers ctx

    /// Content-Types that are acceptable for the response
    let Accept contentType (ctx: HeaderContext) =
        Header.accept contentType ctx

    /// Character sets that are acceptable
    let AcceptCharset characterSets (ctx: HeaderContext) =
        Header.acceptCharset characterSets ctx

    /// Acceptable version in time
    let AcceptDatetime dateTime (ctx: HeaderContext) =
        Header.acceptDatetime dateTime ctx

    /// List of acceptable encodings. See HTTP compression.
    let AcceptEncoding encoding (ctx: HeaderContext) =
        Header.acceptEncoding encoding ctx

    /// List of acceptable human languages for response
    let AcceptLanguage language (ctx: HeaderContext) =
        Header.acceptLanguage language ctx

    /// Authorization credentials for HTTP authorization
    let Authorization credentials (ctx: HeaderContext) =
        Header.authorization credentials ctx

    /// Authorization header using Bearer authorization token
    let AuthorizationBearer token (ctx: HeaderContext) =
        Header.authorizationBearer token ctx

    /// Authorization header using Basic (User/Password) authorization
    let AuthorizationUserPw username password (ctx: HeaderContext) =
        Header.authorizationUserPw username password ctx

    /// Used to specify directives that MUST be obeyed by all caching mechanisms along the request/response chain
    let CacheControl control (ctx: HeaderContext) =
        Header.cacheControl control ctx

    /// What type of connection the user-agent would prefer
    let Connection connection (ctx: HeaderContext) =
        Header.connection connection ctx

    /// An HTTP cookie previously sent by the server with 'Set-Cookie'.
    let Cookie name value (ctx: HeaderContext) =
        Header.cookie name value ctx

    /// An HTTP cookie previously sent by the server with 'Set-Cookie' with
    /// the subset of URIs on the origin server to which this Cookie applies.
    let CookieForPath name value path (ctx: HeaderContext) =
        Header.cookieForPath name value path ctx

    /// An HTTP cookie previously sent by the server with 'Set-Cookie' with
    /// the subset of URIs on the origin server to which this Cookie applies
    /// and the internet domain for which this Cookie is valid.
    let CookieForDomain name value path domain (ctx: HeaderContext) =
        Header.cookieForDomain name value path domain ctx

    /// The date and time that the message was sent
    let Date date (ctx: HeaderContext) =
        Header.date date ctx

    /// Indicates that particular server behaviors are required by the client
    let Expect behaviors (ctx: HeaderContext) =
        Header.expect behaviors ctx

    /// Gives the date/time after which the response is considered stale
    let Expires dateTime (ctx: HeaderContext) =
        Header.expires dateTime ctx

    /// The email address of the user making the request
    let From email (ctx: HeaderContext) =
        Header.from email ctx

    /// The domain name of the server (for virtual hosting), and the TCP port number on which the server is listening.
    /// The port number may be omitted if the port is the standard port for the service requested.
    let Host host (ctx: HeaderContext) =
        Header.host host ctx

    /// Only perform the action if the client supplied entity matches the same entity on the server.
    /// This is mainly for methods like PUT to only update a resource if it has not been modified since the user last updated it.
    let IfMatch entity (ctx: HeaderContext) =
        Header.ifMatch entity ctx

    /// Allows a 304 Not Modified to be returned if content is unchanged
    let IfModifiedSince dateTime (ctx: HeaderContext) =
        Header.ifModifiedSince dateTime ctx

    /// Allows a 304 Not Modified to be returned if content is unchanged
    let IfNoneMatch etag (ctx: HeaderContext) =
        Header.ifNoneMatch etag ctx

    /// If the entity is unchanged, send me the part(s) that I am missing; otherwise, send me the entire new entity
    let IfRange range (ctx: HeaderContext) =
        Header.ifRange range ctx

    /// Only send the response if the entity has not been modified since a specific time
    let IfUnmodifiedSince dateTime (ctx: HeaderContext) =
        Header.ifUnmodifiedSince dateTime ctx

    /// Specifies a parameter used in order to maintain a persistent connection
    let KeepAlive keepAlive (ctx: HeaderContext) =
        Header.keepAlive keepAlive ctx

    /// Specifies the date and time at which the accompanying body data was last modified
    let LastModified dateTime (ctx: HeaderContext) =
        Header.lastModified dateTime ctx

    /// Limit the number of times the message can be forwarded through proxies or gateways
    let MaxForwards count (ctx: HeaderContext) =
        Header.maxForwards count ctx

    /// Initiates a request for cross-origin resource sharing
    /// (asks server for an 'Access-Control-Allow-Origin' response header)
    let Origin origin (ctx: HeaderContext) =
        Header.origin origin ctx

    /// Implementation-specific headers that may have various effects anywhere along the request-response chain.
    let Pragma pragma (ctx: HeaderContext) =
        Header.pragma pragma ctx

    /// Optional instructions to the server to control request processing. 
    /// See RFC https://tools.ietf.org/html/rfc7240 for more details
    let Prefer prefer (ctx: HeaderContext) =
        Header.prefer prefer ctx

    /// Authorization credentials for connecting to a proxy.
    let ProxyAuthorization credentials (ctx: HeaderContext) =
        Header.proxyAuthorization credentials ctx

    /// Request only part of an entity. Bytes are numbered from 0
    let Range start finish (ctx: HeaderContext) =
        Header.range start finish ctx

    /// The address of the previous web page from which a link to the currently requested page was followed.
    /// (The word "referrer" is misspelled in the RFC as well as in most implementations.)
    let Referer referer (ctx: HeaderContext) =
        Header.referer referer ctx

    /// The transfer encodings the user agent is willing to accept: 
    /// the same values as for the response header Transfer-Encoding can be used, 
    /// plus the "trailers" value (related to the "chunked" transfer method) to 
    /// notify the server it expects to receive additional headers after the last chunk.
    let TE te (ctx: HeaderContext) =
        Header.te te ctx

    /// The Trailer general field value indicates that the given set of header fields 
    /// is present in the trailer of a message encoded with chunked transfer-coding
    let Trailer trailer (ctx: HeaderContext) =
        Header.trailer trailer ctx

    /// The TransferEncoding header indicates the form of encoding used to safely
    /// transfer the entity to the user. Valid directives: chunked, compress, deflate, gzip, or identity.
    let TransferEncoding directive (ctx: HeaderContext) =
        Header.transferEncoding directive ctx

    /// Microsoft extension to the HTTP specification used in conjunction with WebDAV functionality.
    let Translate translate (ctx: HeaderContext) =
        Header.translate translate ctx

    /// Specifies additional communications protocols that the client supports.
    let Upgrade upgrade (ctx: HeaderContext) =
        Header.upgrade upgrade ctx

    /// The user agent string of the user agent
    let UserAgent userAgent (ctx: HeaderContext) =
        Header.userAgent userAgent ctx

    /// Informs the server of proxies through which the request was sent
    let Via server (ctx: HeaderContext) =
        Header.via server ctx

    /// A general warning about possible problems with the entity body
    let Warning message (ctx: HeaderContext) =
        Header.warning message ctx

    /// Override HTTP method.
    let XHTTPMethodOverride httpMethod (ctx: HeaderContext) =
        Header.xhttpMethodOverride httpMethod ctx

    /// Custom header
    let Header key value (ctx: HeaderContext) =
        Header.header key value ctx


[<AutoOpen>]
module Body =

    // Use this to transition from a header context to a body definition
    let body (ctx: HeaderContext) =
        (ctx :> IToBodyContext).ToBodyContext()

    /// Content with explicit type and data
    let content contentType data (ctx: BodyContext) =
        Body.content contentType data ctx

    /// Binary data
    let binary data (ctx: BodyContext) =
        Body.binary data ctx

    /// A stream
    let stream stream (ctx: BodyContext) =
        Body.stream stream ctx

    /// An enumerable collection
    let enumerable data (ctx: BodyContext) =
        Body.enumerable data ctx

    /// Text content
    let text text (ctx: BodyContext) =
        Body.text text ctx

    /// JSON content (already in string form)
    let json jsonString (ctx: BodyContext) =
        Body.json jsonString ctx

    /// JSON serialization with custom serializer options
    let jsonSerializeWith options instance (ctx: BodyContext) =
        Body.jsonSerializeWith options instance ctx

    /// JSON serialization using default settings
    let jsonSerialize instance (ctx: BodyContext) =
        Body.jsonSerialize instance ctx

    /// Form URL-encoded data
    let formUrlEncoded data (ctx: BodyContext) =
        Body.formUrlEncoded data ctx

    /// File content (by path)
    let file path (ctx: BodyContext) =
        Body.file path ctx

    /// The type of encoding used on the data
    let ContentEncoding encoding (ctx: BodyContext) =
        Body.contentEncoding encoding ctx

    /// The MIME type of the request body (used with POST and PUT) charset
    let ContentType contentType charset (ctx: BodyContext) =
        Body.contentType contentType charset ctx


[<AutoOpen>]
module Multipart =

    /// An explicit transformation from a previous context to allow for describing the request multiparts.
    let multipart (ctx: HeaderContext) =
        (ctx :> IToMultipartContext).ToMultipartContext()

    /// Creates a text part in a multipart request.
    let textPart value name fileName (ctx: MultipartContext) =
        Multipart.textPart value name fileName ctx

    /// Creates a file part in a multipart request.
    let filePart path name fileName (ctx: MultipartContext) =
        Multipart.filePart path name fileName ctx

    /// Creates a binary part in a multipart request.
    let binaryPart value name fileName (ctx: MultipartContext) =
        Multipart.binaryPart value name fileName ctx

    /// Creates a stream part in a multipart request.
    let streamPart value name fileName (ctx: MultipartContext) =
        Multipart.streamPart value name fileName ctx

    /// Creates an enumerable part in a multipart request.
    let enumerablePart value name fileName (ctx: MultipartContext) =
        Multipart.enumerablePart value name fileName ctx


[<AutoOpen>]
module MultipartElement =

    /// The MIME type of the body of the request (used with POST and PUT requests) and charset
    let ContentTypeMultipart contentType charset (ctx: MultipartElementContext) =
        MultipartElement.contentType contentType charset ctx



#if INTERACTIVE

http {
    POST "https://github.com/CuminAndPotato/PXL-JAM"
    
    AcceptLanguage "en"
    Authorization "credOuter"
    if true then
        Authorization "credInner"

    body
    json """ 
        { 
            name: "PXL Clock",
            description: "A Beautiful and Fun Clock",
            programmingLanguage: [
                "F#", "C#", "JavaScript", "Python", "TypeScript" ]
        } 
    """
    ContentType "application/json"
}



let res =
    http {
        POST "http"
        Accept "application/json"

        multipart

        filePart "src/FsHttp/Domain.fs"
        ContentTypeMultipart "application/fsharp"

        filePart "src/FsHttp/Dsl.fs"

        textPart "das" "hurz1"
        ContentTypeMultipart "application/json"

        // textPart "Lamm" "hurz2"
        // textPart "schrie" "hurz3"
    }


#endif
