using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management_System_WPF.Models
{
    public class PrintItem
    {
        public string Item { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public Dictionary<string, int> DateQty { get; set; } = new();
    }
}
