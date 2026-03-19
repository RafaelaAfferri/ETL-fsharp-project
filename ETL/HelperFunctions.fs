namespace HelperFunctions
open Types
open FSharp.Data

/// <summary>CSV conversion helpers for orders and items.</summary>
module ConvertCsv = 


    /// <summary>Maps a CSV row into an <see cref="T:Types.Order"/> record.</summary>
    /// <param name="row">Row containing order fields.</param>
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
    /// <summary>Converts an orders CSV file into a sequence of <see cref="T:Types.Order"/>.</summary>
    /// <param name="csv">CSV file with headers.</param>
    let CsvToOrder (csv: CsvFile) =
        csv.Rows |> Seq.map(
            rowToOrder
        )


    /// <summary>Maps a CSV row into an <see cref="T:Types.Item"/> record.</summary>
    /// <param name="row">Row containing item fields.</param>
    let rowToItem (row: CsvRow) =
        {
            OrderId = int (row.GetColumn("order_id"))
            ProductId = int (row.GetColumn("product_id"))
            Quantity = int (row.GetColumn("quantity"))
            Price = decimal (row.GetColumn("price"))
            Tax = decimal (row.GetColumn("tax"))
        }   
    /// <summary>Converts an items CSV file into a sequence of <see cref="T:Types.Item"/>.</summary>
    /// <param name="csv">CSV file with headers.</param>
    let CsvToItem (csv: CsvFile) =
        csv.Rows |> Seq.map(
            rowToItem
        )


/// <summary>Filtering and aggregation helpers for orders and items.</summary>
module calculation =


    /// <summary>Filters orders by status.</summary>
    /// <param name="orders">Orders to filter.</param>
    /// <param name="status">Status to keep.</param>
    let filterStatus (orders: seq<Order>) (status: Status) =
        orders |> Seq.filter(fun o -> o.Status = status)

    /// <summary>Filters orders by origin.</summary>
    /// <param name="orders">Orders to filter.</param>
    /// <param name="origin">Origin to keep.</param>
    let filterOrigin (orders: seq<Order>) (origin: Origin) =
        orders |> Seq.filter(fun o -> o.Origin = origin)

    /// <summary>Calculates the total amount for an order.</summary>
    /// <param name="items">Items to aggregate.</param>
    /// <param name="order_id">Order identifier.</param>
    let calculeTotalAmount (items: seq<Item>) (order_id: int) = 
        let filteredItems =  items |> Seq.filter(fun (i: Item) -> i.OrderId = order_id)
        filteredItems |> Seq.map(fun i ->(i.Price * decimal i.Quantity)) |> Seq.sum

    /// <summary>Calculates the total taxes for an order.</summary>
    /// <param name="items">Items to aggregate.</param>
    /// <param name="order_id">Order identifier.</param>
    let calculateTotalTaxes (items: seq<Item>) (order_id: int) =
        let filteredItems =  items |> Seq.filter(fun (i: Item) -> i.OrderId = order_id)
        filteredItems |> Seq.map(fun i ->(i.Tax * decimal i.Quantity * i.Price)) |> Seq.sum







