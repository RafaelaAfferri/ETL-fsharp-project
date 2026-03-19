/// <summary>Console entry point for the ETL pipeline.</summary>
module Main

open Types
open HelperFunctions
open Reader
open Writer
open System
open System.IO

/// <summary>Runs the ETL process and writes aggregated totals.</summary>
/// <param name="argv">Command-line arguments (unused).</param>
[<EntryPoint>]
let main argv =

    /// <summary>Normalizes user input for comparisons.</summary>
    let normalize (value: string) =
        value.Trim().ToLowerInvariant()

    /// <summary>Parses a status string into a <see cref="T:Types.Status"/>.</summary>
    let tryParseStatus (value: string) =
        match normalize value with
        | "pending" -> Some Pending
        | "completed" | "complete" -> Some Completed
        | "cancelled" | "canceled" -> Some Cancelled
        | _ -> None

    /// <summary>Parses an origin string into a <see cref="T:Types.Origin"/>.</summary>
    let tryParseOrigin (value: string) =
        match normalize value with
        | "online" | "o" -> Some Online
        | "person" | "p" -> Some Person
        | _ -> None

    /// <summary>Prompts the user for a yes/no answer.</summary>
    let askYesNo (prompt: string) =
        printf "%s (s/n): " prompt
        let response = Console.ReadLine()
        normalize response = "s"

    /// <summary>Prompts the user for a status filter.</summary>
    let askStatus () =
        printf "Qual status? (Pending/Completed/Cancelled): "
        let response = Console.ReadLine()
        tryParseStatus response

    /// <summary>Prompts the user for an origin filter.</summary>
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


    let outputPath: string = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "DataOut", "order_totals.csv"))
    Writer.writeOrderTotals outputPath orderTotals
    printfn "CSV salvo em: %s" outputPath




    0// Return an integer exit code