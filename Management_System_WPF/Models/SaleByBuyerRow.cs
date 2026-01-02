using System;
using System.Collections.Generic;

namespace Management_System_WPF.Models
{
    public class SaleByBuyerRow
    {
        public DateTime Date { get; set; }

        // ✅ Use DECIMAL everywhere for money
        public Dictionary<string, decimal?> BuyerValues { get; set; } = new();

        public decimal? Total { get; set; }
    }
}
