open Main
open Microsoft.AspNetCore.Http.Internal
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System

let logger = 
    { new ILogger with 
        member _x.Log(_, _, state, _, _) =
            printfn "%A" state
        member _x.IsEnabled  _ = true  
        member _x.BeginScope _ = { new IDisposable with member _x.Dispose() = () } }

[<EntryPoint>]
let main argv =
    let req = DefaultHttpRequest(DefaultHttpContext())
    //fill required HttpRequest parameters here
    run(req, logger)
    |> printfn "%A"
    0 // return an integer exit code