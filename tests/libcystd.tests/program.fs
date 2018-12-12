namespace LibCyStd.Tests

open System
open System.Net
open System.Net.Http

open Expecto
open Expecto.Logging

open LibCyStd

open Newtonsoft.Json

open Optional
open System.Collections.ObjectModel
open System.Collections.Generic

type CookiesResp =
    { Cookies: ReadOnlyDictionary<string, string>}

type PostResp<'a> =
    { Data: 'a
      Headers: ReadOnlyDictionary<string, string>}

module internal Program =
    let parseProxy (input: string) =
        let tryCreate (s: string) =
            try WebProxy (s) |> ValueSome
            with | :? ArgumentException | :? UriFormatException -> ValueNone
        let tryCreate4 (sp: string []) =
            match sp.[0..1] |> String.concat ":" |> tryCreate with
            | ValueNone -> ValueNone
            | ValueSome p -> p.Credentials <- NetworkCredential (sp.[2], sp.[3]); p |> ValueSome
        let tryParse () =
            let sp = input.Split (':')
            match sp.Length with
            | 2 -> tryCreate input
            | 4 -> tryCreate4 sp
            | _ ->  ValueNone
        match input with
        | s when String.IsNullOrWhiteSpace (s) || not <| s.Contains (":") -> ValueNone
        | _ -> tryParse ()

    let env name = Environment.GetEnvironmentVariable (name)

    let proxy () =
        match env "TEST_PROXY" |> parseProxy with
        | ValueNone -> invalidOp "failed to parse proxy from environment variable 'TEST_PROXY'."
        | ValueSome p -> p

    let deserialize<'a> text = JsonConvert.DeserializeObject<'a> (text)

    let post content =
        async {
            let req =
                HttpReq (
                    HttpMethod.Post,
                    "https://httpbin.org/post",
                    Proxy = Option.Some<WebProxy> (proxy ()),
                    Headers = [struct ("Accept", "application/json"); struct ("Content-Type", "application/x-www-form-urlencoded")],
                    ContentBody = Option.Some<_>(content)
                )
            let! resp = Http.RetrRespAsync (req) |> Async.AwaitTask
            Expect.equal resp.StatusCode HttpStatusCode.OK "Http response status code was not OK."
            let _json = deserialize<PostResp<string>> resp.ContentBody
            return ()
        }

    let cookies () =
        async {
            let req =
                HttpReq (
                    HttpMethod.Get,
                    "https://httpbin.org/cookies/set/nam1/val1",
                    Headers = [struct ("Accept", "application/json")],
                    Cookies = [Cookie ("name2", "value2", "/", ".httpbin.org")],
                    Proxy = Option.Some<WebProxy> (proxy ())
                )

            let! resp = Http.RetrRespAsync (req) |> Async.AwaitTask
            Expect.equal resp.StatusCode HttpStatusCode.OK "Http response status code was not OK."
            Expect.equal resp.Cookies.Count 1 "Http response cookies count was not 1."
            Expect.equal resp.Cookies.[0].Name "nam1" "Cookie name was not nam1."
            Expect.equal resp.Cookies.[0].Value "val1" "Cookie value was not val1."

            let _json = deserialize<CookiesResp> resp.ContentBody

            return ()
        }

    let runTests () =
        Tests.testSequenced <| testList "libcystd tests" [
            testCaseAsync "post seq" <| (post <| new EncodedFormValuesHttpContent ([ struct ("hellO!!!", "WORLDDD! T3H P3NGUION OF D00M!!!1!1")]))
            testCaseAsync "cookies" <| cookies ()
        ]

    [<EntryPoint>]
    let main argv =
#if DEBUG
        Global.initialise ({ Global.defaultConfig with getLogger = fun name -> new TextWriterTarget (name, Debug, Console.Out) :> Logger } )
#endif
        let result = runTestsWithArgs defaultConfig argv <| runTests ()
        stdin.ReadLine () |> ignore
        result