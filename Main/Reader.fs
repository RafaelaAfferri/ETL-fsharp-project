/// <summary>CSV readers for input data files.</summary>
module Reader

open FSharp.Data


let  localOrdersPath = __SOURCE_DIRECTORY__ + "/../DataIn/order.csv"
let  localItemsPath = __SOURCE_DIRECTORY__ + "/../DataIn/order_item.csv"

let  remoteOrdersUrl ="https://raw.githubusercontent.com/RafaelaAfferri/ETL-fsharp-project/main/DataIn/order.csv"

let  remoteItemsUrl ="https://raw.githubusercontent.com/RafaelaAfferri/ETL-fsharp-project/main/DataIn/order_item.csv"

/// <summary>Loads orders CSV from local disk or remote URL.</summary>
let loadOrders (path: string) =
    CsvFile.Load(path, hasHeaders = true)

/// <summary>Loads order items CSV from local disk or remote URL.</summary>
let loadItems (path: string) =
    CsvFile.Load(path, hasHeaders = true)