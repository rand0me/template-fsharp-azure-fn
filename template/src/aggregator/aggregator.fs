module Aggregator

open System
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.IO

type TeamStat = 
    { [<JsonProperty("redCards")>]    RedCards: int
      [<JsonProperty("yellowCards")>] YellowCards: int }

type Date = 
    { [<JsonProperty("full")>]     FullDate : DateTime 
      [<JsonProperty("dateType")>] Type     : string }

type Event = 
    { [<JsonProperty("startDate")>] StartDate : Date []
      [<JsonProperty("teamStats")>] TeamStats : TeamStat [] }

type Answer = 
    { HasOneRedCard          : bool
      HasOneYellowCard       : bool
      LastGameCardMessage    : string
      LastGameHasNoCards     : bool
      LastGameRedCards       : int
      LastGameYellowCards    : int
      SeasonDateStart        : DateTime
      SeasonDateEnd          : DateTime
      ShowRedCardsMessage    : bool
      ShowYellowCardsMessage : bool
      Team                   : string }

let getEvents (stream: Stream) = 
    use reader = new StreamReader(stream)
    let json = JObject.Parse (reader.ReadToEnd())
    let teamName = json.SelectToken("$.apiResults[*].league.teams[0].displayName") |> string
    let events = 
        json.SelectTokens("$.apiResults[*].league.teams[*].seasons[*].eventType[*].splits[*].events[*]")
        |> Seq.map (fun j -> j.ToObject<Event>())
        |> Array.ofSeq
    teamName, events

let toLastGameCardMessage redCards yellowCards =
    match redCards, yellowCards with
    | 0, 0 -> "did not receive any cards"
    | 0, 1 -> "received one yellow card"
    | 1, 0 -> "received one red card"
    | _    -> sprintf "received %d cards" (redCards + yellowCards)

let getAnswer (teamName, events) = 
    let orderedEvents =
        events
        |> Array.sortByDescending (fun x -> 
            x.StartDate 
            |> Array.pick (fun d -> if d.Type = "UTC" then Some d.FullDate else None))
    let lastEvent = orderedEvents.[0]
    let lastEventStats = lastEvent.TeamStats.[0]

    let totalCardsLastGame = lastEventStats.RedCards + lastEventStats.YellowCards

    { HasOneRedCard          = lastEventStats.RedCards    = 1
      HasOneYellowCard       = lastEventStats.YellowCards = 1
      LastGameCardMessage    = toLastGameCardMessage lastEventStats.RedCards lastEventStats.YellowCards
      LastGameHasNoCards     = totalCardsLastGame = 0
      LastGameRedCards       = lastEventStats.RedCards
      LastGameYellowCards    = lastEventStats.YellowCards
      SeasonDateStart        = DateTime()//
      SeasonDateEnd          = DateTime()//
      ShowRedCardsMessage    = false//
      ShowYellowCardsMessage = false//
      Team                   = teamName }