using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Management_System_WPF.Helpers
{
    public static class PivotHelper
    {
        // =========================================================
        // SALES + RETURNS PIVOT WITH TOTALS
        // =========================================================
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

            // =====================================================
            // ITEMS FROM SALES + RETURNS
            // =====================================================
            var items = sales
                .Select(x => x.ItemName)
                .Union(returns.Select(r => r.ItemName))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // =====================================================
            // COLUMNS
            // =====================================================
            dt.Columns.Add("Date", typeof(string));
            foreach (var item in items)
                dt.Columns.Add(item, typeof(string)); // string for display

            // =====================================================
            // 1️⃣ DAILY SALES ROWS
            // =====================================================
            var groupedByDate = sales
                .GroupBy(x => x.Date)
                .OrderBy(x => x.Key);

            foreach (var group in groupedByDate)
            {
                DataRow row = dt.NewRow();

                row["Date"] = DateTime.TryParse(group.Key.ToString(), out DateTime d)
                    ? d.ToString("dd-MM-yyyy")
                    : group.Key.ToString();

                foreach (var item in items)
                {
                    int qty = group
                        .Where(x => x.ItemName == item)
                        .Sum(x => x.Qty);

                    row[item] = qty == 0 ? "" : qty.ToString();
                }

                dt.Rows.Add(row);
            }

            // =====================================================
            // 2️⃣ TOTAL QTY ROW
            // =====================================================
            DataRow totalQtyRow = dt.NewRow();
            totalQtyRow["Date"] = "Total Qty";

            var totalQtys = new Dictionary<string, int>();

            foreach (var item in items)
            {
                int total = sales
                    .Where(x => x.ItemName == item)
                    .Sum(x => x.Qty);

                totalQtys[item] = total;
                totalQtyRow[item] = total == 0 ? "" : total.ToString();
            }

            dt.Rows.Add(totalQtyRow);

            // =====================================================
            // 3️⃣ UNIT PRICE ROW
            // =====================================================
            DataRow unitPriceRow = dt.NewRow();
            unitPriceRow["Date"] = "Unit Price";

            var unitPrices = new Dictionary<string, decimal>();
            var dbItems = ItemsService.GetAllItems();

            foreach (var item in items)
            {
                var sale = sales.FirstOrDefault(x => x.ItemName == item);
                decimal price = sale?.Price ?? 0;
                // ✅ Fallback: If not sold this month, get current price from Master DB
                if (price == 0)
                {
                    var dbItem = dbItems.FirstOrDefault(x => x.Name == item);
                    if (dbItem != null) price = dbItem.Price;
                }

                unitPrices[item] = price;
                unitPriceRow[item] = price == 0 ? "" : price.ToString("0.##");
            }

            dt.Rows.Add(unitPriceRow);

            // =====================================================
            // 4️⃣ LESS RETURN ROW (ONLY IF RETURNS EXIST)
            // =====================================================
            var returnValues = new Dictionary<string, decimal>();
            bool hasAnyReturn = returns.Any(r => r.Qty > 0);

            if (hasAnyReturn)
            {
                DataRow returnRow = dt.NewRow();
                returnRow["Date"] = "Less Return";

                foreach (var item in items)
                {
                    int returnQty = returns
                        .Where(r => r.ItemName == item)
                        .Sum(r => r.Qty);

                    if (returnQty > 0 && unitPrices[item] > 0)
                    {
                        decimal value = returnQty * unitPrices[item];
                        returnValues[item] = value;
                        returnRow[item] = $"-{value:0.##}";
                    }
                    else
                    {
                        returnValues[item] = 0;
                        returnRow[item] = "";
                    }
                }

                dt.Rows.Add(returnRow);
            }
            else
            {
                foreach (var item in items)
                    returnValues[item] = 0;
            }

            // =====================================================
            // 5️ TOTAL PRICE ROW (NET)
            // =====================================================
           
            DataRow totalPriceRow = dt.NewRow();
            totalPriceRow["Date"] = "Total Price";

            foreach (var item in items)
            {
                decimal gross = 0;

                // Calculate Gross Sales (if any)
                if (totalQtys.ContainsKey(item) && totalQtys[item] > 0 && unitPrices.ContainsKey(item))
                {
                    gross = totalQtys[item] * unitPrices[item];
                }

                // Get Return Value (default to 0 if missing)
                decimal retValue = returnValues.ContainsKey(item) ? returnValues[item] : 0;

                // Calculate Net
                decimal net = gross - retValue;

                // ✅ FIX: Show value if there is Gross Sales OR Return Value
                // (Previously it might have only checked totalQtys > 0)
                if (gross > 0 || retValue > 0)
                {
                    totalPriceRow[item] = net.ToString("0.##");
                }
                else
                {
                    totalPriceRow[item] = "";
                }
            }

            dt.Rows.Add(totalPriceRow);

            return dt;
        }

        // =========================================================
        // ITEM ROW-WISE TABLE
        // =========================================================
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

        // =========================================================
        // SALES MATRIX WITH TOTALS
        // =========================================================
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
