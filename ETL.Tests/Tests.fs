module Tests

open System
open Xunit
open FSharp.Data
open Types
open HelperFunctions

let private parseCsv (content: string) =
    CsvFile.Parse(content, hasHeaders = true)

let private makeOrder id clientId (date: DateTime) status origin =
    {
        Id = id
        ClientId = clientId
        OrderDate = date
        Status = status
        Origin = origin
    }

// ConvertCsv module tests
[<Fact>]
let ``rowToOrder maps csv row to Order`` () =
    // rowToOrder: single row with Pending + Online mapping.
    let csv =
        "id,client_id,order_date,status,origin\n" +
        "1,10,2024-01-02,Pending,O\n"
    let row = parseCsv csv |> fun f -> f.Rows |> Seq.head

    let order = ConvertCsv.rowToOrder row

    Assert.Equal(1, order.Id)
    Assert.Equal(10, order.ClientId)
    Assert.Equal(DateTime(2024, 1, 2), order.OrderDate)
    Assert.Equal(Pending, order.Status)
    Assert.Equal(Online, order.Origin)

[<Fact>]
let ``CsvToOrder converts all rows`` () =
    // CsvToOrder: multiple rows including Completed + Person mapping.
    let csv =
        "id,client_id,order_date,status,origin\n" +
        "1,10,2024-01-02,Pending,O\n" +
        "2,11,2024-02-03,Complete,P\n"
    let rows = parseCsv csv |> ConvertCsv.CsvToOrder |> Seq.toList

    Assert.Equal(2, rows.Length)
    Assert.Equal(Completed, rows.[1].Status)
    Assert.Equal(Person, rows.[1].Origin)

[<Fact>]
let ``rowToItem maps csv row to Item`` () =
    // rowToItem: single row with decimal fields.
    let csv =
        "order_id,product_id,quantity,price,tax\n" +
        "1,100,2,10.0,0.1\n"
    let row = parseCsv csv |> fun f -> f.Rows |> Seq.head

    let item = ConvertCsv.rowToItem row

    Assert.Equal(1, item.OrderId)
    Assert.Equal(100, item.ProductId)
    Assert.Equal(2, item.Quantity)
    Assert.Equal(10.0m, item.Price)
    Assert.Equal(0.1m, item.Tax)

[<Fact>]
let ``CsvToItem converts all rows`` () =
    // CsvToItem: multiple rows parsed into list.
    let csv =
        "order_id,product_id,quantity,price,tax\n" +
        "1,100,2,10.0,0.1\n" +
        "2,101,1,5.0,0.2\n"
    let rows = parseCsv csv |> ConvertCsv.CsvToItem |> Seq.toList

    Assert.Equal(2, rows.Length)
    Assert.Equal(101, rows.[1].ProductId)

// calculation module tests
[<Fact>]
let ``filterStatus keeps only matching status`` () =
    // filterStatus: keeps only Completed from a mixed list.
    let orders =
        [
            makeOrder 1 10 (DateTime(2024, 1, 1)) Pending Online
            makeOrder 2 11 (DateTime(2024, 1, 2)) Completed Person
        ]

    let filtered = calculation.filterStatus orders Completed |> Seq.toList

    Assert.Single(filtered) |> ignore
    Assert.Equal(2, filtered.[0].Id)

[<Fact>]
let ``filterOrigin keeps only matching origin`` () =
    // filterOrigin: keeps only Online from a mixed list.
    let orders =
        [
            makeOrder 1 10 (DateTime(2024, 1, 1)) Pending Online
            makeOrder 2 11 (DateTime(2024, 1, 2)) Completed Person
        ]

    let filtered = calculation.filterOrigin orders Online |> Seq.toList

    Assert.Single(filtered) |> ignore
    Assert.Equal(1, filtered.[0].Id)

[<Fact>]
let ``calculeTotalAmount sums totals per order`` () =
    // calculeTotalAmount: sums only items for order 1.
    let items =
        [
            { OrderId = 1; ProductId = 100; Quantity = 2; Price = 10.0m; Tax = 0.1m }
            { OrderId = 1; ProductId = 101; Quantity = 1; Price = 5.0m; Tax = 0.2m }
            { OrderId = 2; ProductId = 102; Quantity = 3; Price = 4.0m; Tax = 0.05m }
        ]

    let total = calculation.calculeTotalAmount items 1

    Assert.Equal(25.0m, total)

[<Fact>]
let ``calculateTotalTaxes sums taxes per order`` () =
    // calculateTotalTaxes: sums only items for order 1.
    let items =
        [
            { OrderId = 1; ProductId = 100; Quantity = 2; Price = 10.0m; Tax = 0.1m }
            { OrderId = 1; ProductId = 101; Quantity = 1; Price = 5.0m; Tax = 0.2m }
            { OrderId = 2; ProductId = 102; Quantity = 3; Price = 4.0m; Tax = 0.05m }
        ]

    let total = calculation.calculateTotalTaxes items 1

    Assert.Equal(3.0m, total)

[<Fact>]
let ``buildOrderSummaries returns totals for allowed orders`` () =
    // buildOrderSummaries: filters to allowed orders and aggregates totals.
    let items =
        [
            { OrderId = 1; ProductId = 100; Quantity = 2; Price = 10.0m; Tax = 0.1m }
            { OrderId = 1; ProductId = 101; Quantity = 1; Price = 5.0m; Tax = 0.2m }
            { OrderId = 2; ProductId = 102; Quantity = 3; Price = 4.0m; Tax = 0.05m }
        ]
    let orders =
        [
            makeOrder 1 10 (DateTime(2024, 1, 1)) Pending Online
        ]

    let summaries = calculation.buildOrderSummaries items orders

    Assert.Equal(1, summaries.Count)
    let summary = summaries.[1]
    Assert.Equal(25.0m, summary.TotalAmount)
    Assert.Equal(3.0m, summary.TotalTaxes)
    Assert.Equal(2, summary.Items.Length)

[<Fact>]
let ``CalculateAverageAmountByMonthYear returns averages`` () =
    // CalculateAverageAmountByMonthYear: averages January and February totals.
    let items =
        [
            { OrderId = 1; ProductId = 100; Quantity = 2; Price = 10.0m; Tax = 0.1m }
            { OrderId = 2; ProductId = 101; Quantity = 1; Price = 5.0m; Tax = 0.2m }
            { OrderId = 3; ProductId = 102; Quantity = 3; Price = 4.0m; Tax = 0.05m }
        ]
    let orders =
        [
            makeOrder 1 10 (DateTime(2024, 1, 10)) Pending Online
            makeOrder 2 11 (DateTime(2024, 1, 20)) Completed Person
            makeOrder 3 12 (DateTime(2024, 2, 5)) Cancelled Online
        ]

    let totals = calculation.CalculateAverageAmountByMonthYear items orders |> Map.ofSeq

    Assert.Equal(12.5m, totals.[(1, 2024)])
    Assert.Equal(12.0m, totals.[(2, 2024)])

[<Fact>]
let ``CalculateAverageTaxesByMonthYear returns averages`` () =
    // CalculateAverageTaxesByMonthYear: averages January and February taxes.
    let items =
        [
            { OrderId = 1; ProductId = 100; Quantity = 2; Price = 10.0m; Tax = 0.1m }
            { OrderId = 2; ProductId = 101; Quantity = 1; Price = 5.0m; Tax = 0.2m }
            { OrderId = 3; ProductId = 102; Quantity = 3; Price = 4.0m; Tax = 0.05m }
        ]
    let orders =
        [
            makeOrder 1 10 (DateTime(2024, 1, 10)) Pending Online
            makeOrder 2 11 (DateTime(2024, 1, 20)) Completed Person
            makeOrder 3 12 (DateTime(2024, 2, 5)) Cancelled Online
        ]

    let totals = calculation.CalculateAverageTaxesByMonthYear items orders |> Map.ofSeq

    Assert.Equal(1.5m, totals.[(1, 2024)])
    Assert.Equal(0.6m, totals.[(2, 2024)])

// Parsers module tests
[<Fact>]
let ``tryParseStatus returns Some for valid values`` () =
    // tryParseStatus: accepts mixed case and synonyms.
    Assert.Equal(Some Pending, Parsers.tryParseStatus "Pending")
    Assert.Equal(Some Completed, Parsers.tryParseStatus "complete")
    Assert.Equal(Some Cancelled, Parsers.tryParseStatus "canceled")

[<Fact>]
let ``tryParseStatus returns None for invalid value`` () =
    // tryParseStatus: returns None for unknown status.
    Assert.Equal(None, Parsers.tryParseStatus "unknown")

[<Fact>]
let ``tryParseOrigin returns Some for valid values`` () =
    // tryParseOrigin: accepts short and full forms.
    Assert.Equal(Some Online, Parsers.tryParseOrigin "o")
    Assert.Equal(Some Person, Parsers.tryParseOrigin "person")

[<Fact>]
let ``tryParseOrigin returns None for invalid value`` () =
    // tryParseOrigin: returns None for unknown origin.
    Assert.Equal(None, Parsers.tryParseOrigin "x")
