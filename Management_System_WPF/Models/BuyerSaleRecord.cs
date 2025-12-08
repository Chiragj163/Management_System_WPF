using System;
using System.Collections.Generic;
using System.Text;

namespace Management_System_WPF.Models
{
    public class BuyerSaleRecord
    {
        public string? SaleDate { get; set; }
        public string? ItemName { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public double Total { get; set; }
    }
}

