/// <summary>CSV readers for input data files.</summary>
module Reader

open FSharp.Data


/// <summary>Orders CSV with headers.</summary>
let orders = CsvFile.Load(__SOURCE_DIRECTORY__ + "/../DataIn/order.csv", hasHeaders = true)
/// <summary>Order items CSV with headers.</summary>
let items = CsvFile.Load(__SOURCE_DIRECTORY__ + "/../DataIn/order_item.csv", hasHeaders = true)