using System;

namespace Management_System_WPF.Models
{
    public class SaleRecord
    {
        // Sales table
        public int SaleId { get; set; }
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = "";

        public DateTime SaleDate { get; set; }
        public double TotalAmount { get; set; }

        // Items / Sale Items table
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public int Qty { get; set; }
        public double Price { get; set; }

        // Old code compatibility -----------------------

        // some old pages use Item
        public string Item
        {
            get => ItemName;
            set => ItemName = value;
        }

        // some old pages use Quantity
        public int Quantity
        {
            get => Qty;
            set => Qty = value;
        }

        // this is where the error was – now it has a setter
        public double Amount { get; set; }

        // some code may use Total – allow set & also give a computed helper
        public double Total { get; set; }

        // string Date used in old code / bindings
        public string Date
        {
            get => SaleDate.ToString("yyyy-MM-dd");
            set => SaleDate = DateTime.Parse(value);
        }
    }
}
