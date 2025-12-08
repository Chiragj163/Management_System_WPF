using System;
using System.Collections.Generic;
using System.Text;

namespace Management_System_WPF.Models
{
    public class ArticleSaleRow
    {
        public string Date { get; set; }
        public Dictionary<string, double> ArticleValues { get; set; }

        public ArticleSaleRow()
        {
            ArticleValues = new Dictionary<string, double>();
        }
    }

}
