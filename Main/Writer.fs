module Writer

open System
open System.Globalization
open System.IO
open Types


let private toInvariantString (value: decimal) =
    value.ToString(CultureInfo.InvariantCulture)

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
