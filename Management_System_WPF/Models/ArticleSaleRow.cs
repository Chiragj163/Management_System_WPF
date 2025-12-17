using System;
using System.Collections.Generic;
using System.Linq;

namespace Management_System_WPF.Models
{
    public class ArticleSaleRow
    {
        public DateTime Date { get; set; }

        public Dictionary<string, int?> ArticleValues { get; set; } = new();

        public bool IsTotalRow { get; set; }

        public int Total => ArticleValues.Values.Sum(v => v ?? 0);

        // 👇 Display helper
        public string DateDisplay =>
            IsTotalRow ? "Total" : Date.ToString("dd/MM/yyyy");
    }






}
