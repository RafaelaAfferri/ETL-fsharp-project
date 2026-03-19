namespace HelperFunctions
open Types
open FSharp.Data

module ConvertCsv = 


    let rowToOrder (row: CsvRow) =
        {
            Id = int (row.GetColumn("id"))
            ClientId = int (row.GetColumn("client_id"))
            OrderDate = System.DateTime.Parse(row.GetColumn("order_date"))
            Status = match row.GetColumn("status") with
                        | "Pending" -> Pending
                        | "Complete" | "Completed" -> Completed
                        | "Cancelled" -> Cancelled
                        | _ -> failwith "Unknown status"
            Origin = match row.GetColumn("origin") with
                        | "O" -> Online
                        | "P" -> Person
                        | _ -> failwith "Unknown origin"
        }
    let CsvToOrder (csv: CsvFile) =
        csv.Rows |> Seq.map(
            rowToOrder
        )


    let rowToItem (row: CsvRow) =
        {
            OrderId = int (row.GetColumn("order_id"))
            ProductId = int (row.GetColumn("product_id"))
            Quantity = int (row.GetColumn("quantity"))
            Price = decimal (row.GetColumn("price"))
            Tax = decimal (row.GetColumn("tax"))
        }   
    let CsvToItem (csv: CsvFile) =
        csv.Rows |> Seq.map(
            rowToItem
        )


module calculation =


    let filterStatus (orders: seq<Order>) (status: Status) =
        orders |> Seq.filter(fun o -> o.Status = status)

    let filterOrigin (orders: seq<Order>) (origin: Origin) =
        orders |> Seq.filter(fun o -> o.Origin = origin)

    let calculeTotalAmount (items: seq<Item>) (order_id: int) = 
        let filteredItems =  items |> Seq.filter(fun (i: Item) -> i.OrderId = order_id)
        filteredItems |> Seq.map(fun i ->(i.Price * decimal i.Quantity)) |> Seq.sum

    let calculateTotalTaxes (items: seq<Item>) (order_id: int) =
        let filteredItems =  items |> Seq.filter(fun (i: Item) -> i.OrderId = order_id)
        filteredItems |> Seq.map(fun i ->(i.Tax * decimal i.Quantity * i.Price)) |> Seq.sum







