using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management_System_WPF.Models
{
    public class ReturnModel
    {
        public int ReturnId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Qty { get; set; }
    }
}