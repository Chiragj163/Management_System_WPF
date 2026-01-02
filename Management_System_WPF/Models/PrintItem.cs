using System;
using System.Collections.Generic;

public class PrintItem
{
    // Display text (what you show in header)
    public string Item { get; set; } = "";

    // Normalized key used for matching sales vs returns
    public string ItemKey { get; set; } = "";

    public decimal UnitPrice { get; set; }

    public int TotalQty { get; set; }          // Sales Qty
    public int TotalReturns { get; set; }      // Return Qty (positive number)

    public int NetQty => TotalQty - TotalReturns;

    public decimal NetAmount => NetQty * UnitPrice;

    // Daily sales qty
    public Dictionary<DateTime, int> DateQty { get; set; } = new();
}
