using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    namespace Management_System_WPF.Models
    {
        public class ItemPriceHistory
        {
            public int Id { get; set; }
            public int ItemId { get; set; }
            public decimal OldPrice { get; set; }
            public decimal NewPrice { get; set; }
            public DateTime ChangedOn { get; set; }
        }
    }



