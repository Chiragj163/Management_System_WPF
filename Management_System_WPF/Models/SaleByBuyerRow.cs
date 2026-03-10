using System;
using System.Collections.Generic;

namespace Management_System_WPF.Models
{
    public class SaleByBuyerRow
    {
        public DateTime Date { get; set; }
        public Dictionary<string, decimal?> BuyerValues { get; set; } = new();

        public decimal? Total { get; set; }
    }
}
