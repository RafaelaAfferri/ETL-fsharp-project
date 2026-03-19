namespace Types

type Status = 
    | Pending
    | Completed
    | Cancelled

type Origin = 
    | Online
    | Person

type Order = {
    Id: int
    ClientId: int
    OrderDate: System.DateTime
    Status: Status
    Origin: Origin
}

type Item = {
    OrderId: int
    ProductId: int
    Quantity: int
    Price: decimal
    Tax: decimal
}

type OrderTotals = {
    OrderId: int
    TotalAmount: decimal
    TotalTaxes: decimal
}
