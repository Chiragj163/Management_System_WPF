using System;

public class SaleRecord
{
    public int SaleId { get; set; }
    public int SaleItemId { get; set; }

    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = "";

    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }  

    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int Qty { get; set; }
    public decimal Price { get; set; }      
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

    public decimal Amount { get; set; }        
    public decimal Total { get; set; }      

    public string Date
    {
        get => SaleDate.ToString("yyyy-MM-dd");
        set => SaleDate = DateTime.Parse(value);
    }
}
