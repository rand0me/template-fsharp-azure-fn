module Main

open System
open System.Net
open System.IO
open System.Web.Http

open Aggregator
open Hopac
open Hopac.IO.Azure
open Hopac.IO.Datalake
open Hopac.IO.Stream
open Hopac.Infixes

open Newtonsoft.Json
open Newtonsoft.Json.Linq

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Mvc

let AppId              = "6af3b625-5a42-419c-ad60-295f3976b926"
let AppSecret          = "TdSarozNB2zXBKtNiNwqHJXfYKkFX801GuO3Zl6jT74="
let TenantId           = "7945008c-5c1b-42a9-a1c3-7f3d3642da94"
let StorageAccountName = "arkadium"
let DataLakeFolder     = "/data/inhabit/raw/sports/soccer/stats/"
let ResultPath         = "/data/inhabit/aggregated/soccer-red-cards/result.json"

type DataRequests = 
    | AllTeamsOfCurrentEplSeason
    | TeamLogByGame of int

let toDataLakePath = function
    | AllTeamsOfCurrentEplSeason -> Path.Combine (DataLakeFolder, "teams-epl.json")
    | TeamLogByGame    teamId    -> Path.Combine (DataLakeFolder, sprintf "team-log-bydate/%d.json" teamId)

let createDatalake (log: ILogger) =
    job {
        let! possiblyToken = getAdTokenJob <| fun p -> 
            { p with 
                ClientId     = AppId
                ClientSecret = AppSecret
                TenantId     = TenantId }

        match possiblyToken with
        | Choice2Of2 exn   -> return! Job.raises exn     
        | Choice1Of2 token ->

        let uploader = fun path content ->
            log.LogInformation ("uploading to: " + path)
            uploadJob (fun p -> { p with Overwrite = Some true })
                (StorageName  StorageAccountName,
                 DataLakePath path,
                 AccessToken  token.AccessToken,
                 content)

        let downloader = fun path ->
            log.LogInformation ("downloading from: " + path)
            downloadStreamJob 
                (StorageName  StorageAccountName,
                 DataLakePath path,
                 AccessToken  token.AccessToken)
        return uploader, downloader
    }

let get downloader dataRequest = 
    downloader (toDataLakePath dataRequest)
    >>= function
    | Choice1Of2 stream -> Job.result stream
    | Choice2Of2 exn    -> Job.raises exn

let post uploader path content = 
    uploader path content
    >>= function
    | Choice1Of2 (x: HttpWebResponse) when x.StatusCode = HttpStatusCode.Created -> Job.unit()
    | Choice1Of2 (x: HttpWebResponse) -> 
        Job.raises (Exception(sprintf "upload failed with code: %A" x.StatusCode))
    | Choice2Of2 exn -> Job.raises exn

let getTeamIdFromJson jsonString =
    let json = JObject.Parse jsonString
    json.SelectTokens("$.apiResults[*].league.season.conferences[*].divisions[*].teams[*].teamId") 
    |> Seq.map int

let cons t h = List.Cons(h,t)

let run (req: HttpRequest, log: ILogger) =
    try
        log.LogInformation("soccer red cards aggregator started")

        let ul, dl = createDatalake log |> run
        log.LogInformation("established connect to datalake")

        get dl AllTeamsOfCurrentEplSeason
        >>= fun stream -> stream.ReadToEndJob() >>- getTeamIdFromJson
        >>- Stream.ofSeq
        >>- Stream.mapJob  (TeamLogByGame >> get dl >-> getEvents >-> getAnswer)
        >>- Stream.foldFun  cons []
        >>= Job.bind       (JsonConvert.SerializeObject >> StringContent >> (post ul ResultPath))
        |> run

        log.LogInformation("soccer red cards results uploaded to DL")
        OkObjectResult() :> IActionResult
    with
    | e -> 
        log.LogError(e.Message, e)
        InternalServerErrorResult() :> IActionResult