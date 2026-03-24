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

    /// <summary>Groups items by order and calculates totals with item lists.</summary>
    /// <param name="items">Items to group.</param>
    /// <param name="orders">Orders to include in the result.</param>
    let buildOrderSummaries (items: seq<Item>) (orders: seq<Order>) : Map<int, OrderSummary> =
        let allowedOrderIds = orders |> Seq.map (fun o -> o.Id) |> Set.ofSeq
        items
        |> Seq.filter (fun i -> allowedOrderIds |> Set.contains i.OrderId)
        |> Seq.groupBy (fun i -> i.OrderId)
        |> Seq.map (fun (orderId, orderItems) ->
            let itemsList = orderItems |> Seq.toList
            let totalAmount = itemsList |> List.sumBy (fun i -> i.Price * decimal i.Quantity)
            let totalTaxes = itemsList |> List.sumBy (fun i -> i.Tax * decimal i.Quantity * i.Price)
            orderId,
            {
                OrderId = orderId
                TotalAmount = totalAmount
                TotalTaxes = totalTaxes
                Items = itemsList
            }
        )
        |> Map.ofSeq

    /// <summary>Calculates the average amount by month and year.</summary>
    /// <param name="items">Items to aggregate.</param>
    /// <param name="orders">Orders to group by month and year.</param>
    let CalculateAverageAmountByMonthYear (items: seq<Item>) (orders: seq<Order>) =
        let orderTotals =
            orders
            |> Seq.map (fun o ->
                let totalAmount = calculeTotalAmount items o.Id
                (o.OrderDate.Month, o.OrderDate.Year), totalAmount
            )
        orderTotals
        |> Seq.groupBy fst
        |> Seq.map (fun (monthYear, totals) ->
            let sum = totals |> Seq.sumBy snd
            let count = totals |> Seq.length |> decimal
            monthYear, (sum / count)
        )
    
    /// <summary>Calculates the average taxes by month and year.</summary>
    /// <param name="items">Items to aggregate.</param>
    /// <param name="orders">Orders to group by month and year.</param>
    let CalculateAverageTaxesByMonthYear (items: seq<Item>) (orders: seq<Order>) =
        let orderTaxes =
            orders
            |> Seq.map (fun o ->
                let totalTaxes = calculateTotalTaxes items o.Id
                (o.OrderDate.Month, o.OrderDate.Year), totalTaxes
            )
        orderTaxes
        |> Seq.groupBy fst
        |> Seq.map (fun (monthYear, taxes) ->
            let sum = taxes |> Seq.sumBy snd
            let count = taxes |> Seq.length |> decimal
            monthYear, (sum / count)
        )
        
module Parsers =
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







