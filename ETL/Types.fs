namespace Types

/// <summary>Status of an order.</summary>
type Status = 
    | Pending
    | Completed
    | Cancelled

/// <summary>Origin channel for an order.</summary>
type Origin = 
    | Online
    | Person

/// <summary>Order header data from the input CSV.</summary>
type Order = {
    Id: int
    ClientId: int
    OrderDate: System.DateTime
    Status: Status
    Origin: Origin
}

/// <summary>Order item data from the input CSV.</summary>
type Item = {
    OrderId: int
    ProductId: int
    Quantity: int
    Price: decimal
    Tax: decimal
}

/// <summary>Aggregated totals for an order.</summary>
type OrderTotals = {
    OrderId: int
    TotalAmount: decimal
    TotalTaxes: decimal
}
