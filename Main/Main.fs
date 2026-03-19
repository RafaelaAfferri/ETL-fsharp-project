module Main

open Types
open HelperFunctions
open Reader
open Writer
open System
open System.IO

[<EntryPoint>]
let main argv =

    let normalize (value: string) =
        value.Trim().ToLowerInvariant()

    let tryParseStatus (value: string) =
        match normalize value with
        | "pending" -> Some Pending
        | "completed" | "complete" -> Some Completed
        | "cancelled" | "canceled" -> Some Cancelled
        | _ -> None

    let tryParseOrigin (value: string) =
        match normalize value with
        | "online" | "o" -> Some Online
        | "person" | "p" -> Some Person
        | _ -> None

    let askYesNo (prompt: string) =
        printf "%s (s/n): " prompt
        let response = Console.ReadLine()
        normalize response = "s"

    let askStatus () =
        printf "Qual status? (Pending/Completed/Cancelled): "
        let response = Console.ReadLine()
        tryParseStatus response

    let askOrigin () =
        printf "Qual origem? (Online/Person): "
        let response = Console.ReadLine()
        tryParseOrigin response

   
    let ordersList = HelperFunctions.ConvertCsv.CsvToOrder Reader.orders |> Seq.toList
   
    let itemsList = HelperFunctions.ConvertCsv.CsvToItem Reader.items |> Seq.toList

    let filteredOrders =
        let statusFilter =
            if askYesNo "Filtrar por status?" then askStatus () else None

        let originFilter =
            if askYesNo "Filtrar por origem?" then askOrigin () else None

        ordersList
        |> List.filter (fun o ->
            let statusOk =
                match statusFilter with
                | Some s -> o.Status = s
                | None -> true
            let originOk =
                match originFilter with
                | Some o2 -> o.Origin = o2
                | None -> true
            statusOk && originOk
        )


    let orderTotals =
        filteredOrders
        |> List.map (fun o ->
            let totalAmount = HelperFunctions.calculation.calculeTotalAmount itemsList o.Id
            let totalTaxes = HelperFunctions.calculation.calculateTotalTaxes itemsList o.Id
            {
                OrderId = o.Id
                TotalAmount = totalAmount
                TotalTaxes = totalTaxes
            }
        )


    let outputPath = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "DataOut", "order_totals.csv"))
    Writer.writeOrderTotals outputPath orderTotals
    printfn "CSV salvo em: %s" outputPath




    0// Return an integer exit code