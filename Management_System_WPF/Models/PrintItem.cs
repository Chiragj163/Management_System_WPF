using System;
using System.Collections.Generic;

public class PrintItem
{
    public string Item { get; set; } = "";
    public string ItemKey { get; set; } = "";

    public decimal UnitPrice { get; set; }

    public int TotalQty { get; set; }          
    public int TotalReturns { get; set; }   
    public int NetQty => TotalQty - TotalReturns;

    public decimal NetAmount => NetQty * UnitPrice;
    public Dictionary<DateTime, int> DateQty { get; set; } = new();
}
