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

    /// <summary>Prompts the user for a yes/no answer.</summary>
    let askYesNo (prompt: string) =
        printf "%s (s/n): " prompt
        let response = Console.ReadLine()
        HelperFunctions.Parsers.normalize response = "s"

    /// <summary>Prompts the user for a status filter.</summary>
    let askStatus () =
        printf "Qual status? (Pending/Completed/Cancelled): "
        let response = Console.ReadLine()
        HelperFunctions.Parsers.tryParseStatus response

    /// <summary>Prompts the user for an origin filter.</summary>
    let askOrigin () =
        printf "Qual origem? (Online/Person): "
        let response = Console.ReadLine()
        HelperFunctions.Parsers.tryParseOrigin response

    let useInternet = askYesNo "Ler arquivos da internet?"

    let ordersPath = if useInternet then Reader.remoteOrdersUrl else Reader.localOrdersPath
    let itemsPath = if useInternet then Reader.remoteItemsUrl else Reader.localItemsPath

    let ordersList =
        Reader.loadOrders ordersPath
        |> HelperFunctions.ConvertCsv.CsvToOrder
        |> Seq.toList

    let itemsList =
        Reader.loadItems itemsPath
        |> HelperFunctions.ConvertCsv.CsvToItem
        |> Seq.toList

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

    let orderSummaries =
        HelperFunctions.calculation.buildOrderSummaries itemsList filteredOrders

    let orderTotals =
        orderSummaries
        |> Map.toSeq
        |> Seq.map snd
        |> Seq.map (fun s ->
            {
                OrderId = s.OrderId
                TotalAmount = s.TotalAmount
                TotalTaxes = s.TotalTaxes
            }
        )

    let orderTotalsByMonthYear = HelperFunctions.calculation.CalculateTotalAmountByMonthYear itemsList filteredOrders
    let taxesByMonthYear = HelperFunctions.calculation.CalculateTotalTaxesByMonthYear itemsList filteredOrders

    let outputPath: string = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "DataOut", "order_totals.csv"))
    let outputPathByMonthYear: string = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "DataOut", "totals_by_month_year.csv"))
    let dbPath: string = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "DataOut", "etl.db"))

    Writer.writeOrderTotals outputPath orderTotals
    Writer.writeTotalsByMonthYear outputPathByMonthYear orderTotalsByMonthYear taxesByMonthYear
    printfn "CSV salvo em: %s" outputPath

    if askYesNo "Salvar no banco local (SQLite)?" then
        Writer.writeOrderTotalsToSqlite dbPath orderTotals
        printfn "SQLite salvo em: %s" dbPath

    0