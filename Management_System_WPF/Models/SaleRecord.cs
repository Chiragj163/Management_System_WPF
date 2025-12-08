using System;
using System.Collections.Generic;
using System.Text;

namespace Management_System_WPF.Models
{
    public class SaleRecord
    {
        public int SaleId { get; set; }
        public int SaleItemId { get; set; }
        public string BuyerName { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public string SaleDate { get; set; } = "";
    }

}
