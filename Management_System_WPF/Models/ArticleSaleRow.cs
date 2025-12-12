using System;
using System.Collections.Generic;

namespace Management_System_WPF.Models
{
    public class ArticleSaleRow
    {
        public string Date { get; set; }

        // Qty should be INT
        public Dictionary<string, int> ArticleValues { get; set; }

        public ArticleSaleRow()
        {
            ArticleValues = new Dictionary<string, int>();
        }
    }
}
