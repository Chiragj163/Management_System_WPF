using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Documents;

namespace Management_System_WPF.Helpers
{
    public static class PivotHelper
    {
        public static DataTable CreatePivotTableWithTotals(
            List<SalesRaw> sales,
            List<SalesRaw> returns)
        {
            DataTable dt = new DataTable();

            sales ??= new List<SalesRaw>();
            returns ??= new List<SalesRaw>();

            if (sales.Count == 0 && returns.Count == 0)
            {
                dt.Columns.Add("Date", typeof(string));
                dt.Rows.Add("No sales found");
                return dt;
            }
            var items = sales
                .Select(x => x.ItemName)
                .Union(returns.Select(r => r.ItemName))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            dt.Columns.Add("Date", typeof(string));
            foreach (var item in items)
                dt.Columns.Add(item, typeof(string));
            var groupedByDate = sales
                .GroupBy(x => x.Date)
                .OrderBy(x => x.Key);

            foreach (var group in groupedByDate)
            {
                DataRow row = dt.NewRow();

                row["Date"] = DateTime.TryParse(group.Key, out DateTime d)
                    ? d.ToString("dd-MM-yyyy")
                    : group.Key;

                foreach (var item in items)
                {
                    var values = group
                        .Where(x => x.ItemName == item)
                        .Select(x => x.Qty.ToString())
                        .ToList();

                    row[item] = values.Count == 0 ? "" : string.Join(Environment.NewLine, values);
                }

                dt.Rows.Add(row);
            }
            bool hasAnyReturn = returns.Any(r => r.Qty > 0);
            var returnQtys = items.ToDictionary(item => item, item =>
                returns.Where(r => r.ItemName == item).Sum(r => r.Qty));
            if (hasAnyReturn) { 
                DataRow returnRow = dt.NewRow();
            returnRow["Date"] = "Less Return";

            foreach (var item in items)
                returnRow[item] = returnQtys[item] > 0 ? $"-{returnQtys[item]}" : "";

            dt.Rows.Add(returnRow);
            }
            else
            { foreach (var item in items) returnQtys[item] = 0; }
            DataRow totalQtyRow = dt.NewRow();
            totalQtyRow["Date"] = "Total Qty";

            var soldQtys = items.ToDictionary(item => item,
                item => sales.Where(s => s.ItemName == item).Sum(s => s.Qty));

            var netQtys = items.ToDictionary(item => item,
                item => soldQtys[item] - returnQtys[item]);

            foreach (var item in items)
                totalQtyRow[item] = netQtys[item] != 0 ? netQtys[item].ToString() : "";

            dt.Rows.Add(totalQtyRow);

            DataRow unitPriceRow = dt.NewRow();
            unitPriceRow["Date"] = "Unit Price";

            var dbItems = ItemsService.GetAllItems() ?? new List<Item>();
            var unitPrices = new Dictionary<string, decimal>();

            foreach (var item in items)
            {
                decimal price =
                    sales.FirstOrDefault(s => s.ItemName == item)?.Price ??
                    dbItems.FirstOrDefault(d => d.Name == item)?.Price ?? 0;

                unitPrices[item] = price;
                unitPriceRow[item] = price > 0 ? price.ToString("X 0.##") : "";
            }

            dt.Rows.Add(unitPriceRow);

            DataRow totalPriceRow = dt.NewRow();
            totalPriceRow["Date"] = "Total Price";

            foreach (var item in items)
            {
                int qty = netQtys[item];
                decimal price = unitPrices[item];

                if (qty != 0 && price > 0)
                    totalPriceRow[item] = (qty * price).ToString("₹ 0.##");
                else
                    totalPriceRow[item] = "";
            }

            dt.Rows.Add(totalPriceRow);


            return dt;
        }
        public static DataTable CreateItemRowWiseTable(List<SalesRaw> raw)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Item", typeof(string));
            dt.Columns.Add("Qty", typeof(decimal));

            if (raw == null || raw.Count == 0)
                return dt;

            foreach (var r in raw)
            {
                DataRow row = dt.NewRow();

                row["Date"] = DateTime.TryParse(r.Date.ToString(), out DateTime d)
                    ? d.ToString("dd-MMM-yyyy")
                    : r.Date.ToString();

                row["Item"] = r.ItemName;
                row["Qty"] = r.Qty;

                dt.Rows.Add(row);
            }

            DataRow totalRow = dt.NewRow();
            totalRow["Item"] = "TOTAL";
            totalRow["Qty"] = raw.Sum(x => x.Qty);
            dt.Rows.Add(totalRow);

            return dt;
        }

        public static DataTable CreateSalesMatrixWithTotals(List<SalesRaw> raw)
        {
            DataTable dt = new DataTable();

            if (raw == null || raw.Count == 0)
                return dt;

            var items = raw
                .Select(x => x.ItemName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            dt.Columns.Add("Date", typeof(string));
            foreach (var item in items)
                dt.Columns.Add(item, typeof(int));

            dt.Columns.Add("Total", typeof(int));

            var groupedByDate = raw.GroupBy(x => x.Date).OrderBy(x => x.Key);

            foreach (var dateGroup in groupedByDate)
            {
                DataRow row = dt.NewRow();

                row["Date"] = DateTime.TryParse(dateGroup.Key.ToString(), out DateTime d)
                    ? d.ToString("dd-MM-yyyy")
                    : dateGroup.Key.ToString();

                int rowTotal = 0;

                foreach (var item in items)
                {
                    int qty = dateGroup
                        .Where(x => x.ItemName == item)
                        .Sum(x => x.Qty);

                    if (qty > 0)
                    {
                        row[item] = qty;
                        rowTotal += qty;
                    }
                }

                row["Total"] = rowTotal;
                dt.Rows.Add(row);
            }

            DataRow totalRow = dt.NewRow();
            totalRow["Date"] = "Total";

            int grandTotal = 0;

            foreach (var item in items)
            {
                int colTotal = dt.AsEnumerable()
                    .Where(r => r["Date"].ToString() != "Total")
                    .Sum(r => r[item] == DBNull.Value ? 0 : Convert.ToInt32(r[item]));

                totalRow[item] = colTotal;
                grandTotal += colTotal;
            }

            totalRow["Total"] = grandTotal;
            dt.Rows.Add(totalRow);

            return dt;
        }
    }
}
