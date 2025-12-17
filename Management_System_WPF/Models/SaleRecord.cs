using System;

public class SaleRecord
{
    //  PRIMARY KEYS
    public int SaleId { get; set; }
    public int SaleItemId { get; set; }   

    // Buyer
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = "";

    // Sale
    public DateTime SaleDate { get; set; }
    public double TotalAmount { get; set; }

    // Item
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int Qty { get; set; }
    public double Price { get; set; }

    // Backward compatibility
    public string Item
    {
        get => ItemName;
        set => ItemName = value;
    }

    public int Quantity
    {
        get => Qty;
        set => Qty = value;
    }

    public double Amount { get; set; }
    public double Total { get; set; }

    public string Date
    {
        get => SaleDate.ToString("yyyy-MM-dd");
        set => SaleDate = DateTime.Parse(value);
    }
}
