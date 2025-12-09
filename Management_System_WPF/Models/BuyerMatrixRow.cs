using System;
using System.Collections.Generic;
using System.Text;

namespace Management_System_WPF.Models
{
    public class BuyerMatrixRow
    {
        public string Date { get; set; }
        public Dictionary<string, double> BuyerTotals { get; set; }

        public BuyerMatrixRow(string date)
        {
            Date = date;
            BuyerTotals = new Dictionary<string, double>();
        }
    }

}
