/// <summary>CSV writers for output data files.</summary>
module Writer

open System
open System.Globalization
open System.IO
open Microsoft.Data.Sqlite
open Types


/// <summary>Formats a decimal using invariant culture.</summary>
let private toInvariantString (value: decimal) =
    value.ToString(CultureInfo.InvariantCulture)

/// <summary>Writes order totals to a CSV file.</summary>
/// <param name="path">Output file path.</param>
/// <param name="rows">Totals to write.</param>
let writeOrderTotals (path: string) (rows: seq<OrderTotals>) =
    let directory = Path.GetDirectoryName(path)
    if not (String.IsNullOrWhiteSpace(directory)) then
        Directory.CreateDirectory(directory) |> ignore

    use writer = new StreamWriter(path, false)
    writer.WriteLine("order_id,total_amount,total_taxes")
    rows
    |> Seq.iter (fun r ->
        writer.WriteLine(String.Join(",", [|
            r.OrderId.ToString()
            toInvariantString r.TotalAmount
            toInvariantString r.TotalTaxes
        |]))
    )

let writeTotalsByMonthYear (path: string) (OrdersRow: seq<(int * int) * decimal>) (TaxesRow: seq<(int * int) * decimal>) =
    let directory = Path.GetDirectoryName(path)
    if not (String.IsNullOrWhiteSpace(directory)) then
        Directory.CreateDirectory(directory) |> ignore

    use writer = new StreamWriter(path, false)
    writer.WriteLine("month,year,total_amount,total_taxes")
    let data = 
        OrdersRow
        |> Seq.map (fun ((month, year), totalAmount) ->
            let totalTaxes = TaxesRow |> Seq.tryFind (fun ((m, y), _) -> m = month && y = year) |> Option.map snd |> Option.defaultValue 0m
            (month, year, totalAmount, totalTaxes)
        )
    data
    |> Seq.iter (fun (month, year, totalAmount, totalTaxes) ->
        writer.WriteLine(String.Join(",", [|
            month.ToString()
            year.ToString()
            toInvariantString totalAmount
            toInvariantString totalTaxes
        |]))
    )

/// <summary>Writes order totals to a local SQLite database.</summary>
/// <param name="dbPath">SQLite file path.</param>
/// <param name="rows">Totals to write.</param>
let writeOrderTotalsToSqlite (dbPath: string) (rows: seq<OrderTotals>) =
    let directory = Path.GetDirectoryName(dbPath)
    if not (String.IsNullOrWhiteSpace(directory)) then
        Directory.CreateDirectory(directory) |> ignore

    use connection = new SqliteConnection($"Data Source={dbPath}")
    connection.Open()

    use createCmd = connection.CreateCommand()
    createCmd.CommandText <-
        "CREATE TABLE IF NOT EXISTS order_totals (" +
        "order_id INTEGER PRIMARY KEY, " +
        "total_amount REAL NOT NULL, " +
        "total_taxes REAL NOT NULL" +
        ")"
    createCmd.ExecuteNonQuery() |> ignore

    use tx = connection.BeginTransaction()

    use clearCmd = connection.CreateCommand()
    clearCmd.Transaction <- tx
    clearCmd.CommandText <- "DELETE FROM order_totals"
    clearCmd.ExecuteNonQuery() |> ignore

    use insertCmd = connection.CreateCommand()
    insertCmd.Transaction <- tx
    insertCmd.CommandText <-
        "INSERT INTO order_totals (order_id, total_amount, total_taxes) " +
        "VALUES ($order_id, $total_amount, $total_taxes)"

    let orderIdParam = insertCmd.CreateParameter()
    orderIdParam.ParameterName <- "$order_id"
    insertCmd.Parameters.Add(orderIdParam) |> ignore

    let totalAmountParam = insertCmd.CreateParameter()
    totalAmountParam.ParameterName <- "$total_amount" 
    insertCmd.Parameters.Add(totalAmountParam) |> ignore

    let totalTaxesParam = insertCmd.CreateParameter()
    totalTaxesParam.ParameterName <- "$total_taxes"
    insertCmd.Parameters.Add(totalTaxesParam) |> ignore

    rows
    |> Seq.iter (fun r ->
        orderIdParam.Value <- r.OrderId
        totalAmountParam.Value <- r.TotalAmount
        totalTaxesParam.Value <- r.TotalTaxes
        insertCmd.ExecuteNonQuery() |> ignore
    )

    tx.Commit()
    
