/// <summary>CSV writers for output data files.</summary>
module Writer

open System
open System.Globalization
open System.IO
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
