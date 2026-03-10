using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Management_System_WPF.Models
{
    public class CartItem
    {
        public int BuyerId { get; set; }
        public string BuyerName { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

