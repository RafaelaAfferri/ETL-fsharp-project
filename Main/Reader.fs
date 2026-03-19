module Reader

open FSharp.Data


let orders = CsvFile.Load(__SOURCE_DIRECTORY__ + "/../DataIn/order.csv", hasHeaders = true)
let items = CsvFile.Load(__SOURCE_DIRECTORY__ + "/../DataIn/order_item.csv", hasHeaders = true)