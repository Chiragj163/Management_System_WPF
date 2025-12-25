using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management_System_WPF.Models
{
    public class SpecialPriceVM
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SpecialPrice { get; set; }
    }
}
