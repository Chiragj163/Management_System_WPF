using System;
using System.Collections.Generic;
using System.Linq;

namespace Management_System_WPF.Models
{
    public class ArticleSaleRow
    {
        public DateTime Date { get; set; }

        public Dictionary<string, string> ArticleValues { get; set; } = new();

        public Dictionary<string, string> ArticleTooltips { get; set; } = new();

        public bool IsTotalRow { get; set; }

        public int Total =>
            ArticleValues.Values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .SelectMany(v => v.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(x => int.Parse(x))
                .Sum();

        public string DateDisplay =>
            IsTotalRow ? "Total" : Date.ToString("dd/MM/yyyy");
    }







}
