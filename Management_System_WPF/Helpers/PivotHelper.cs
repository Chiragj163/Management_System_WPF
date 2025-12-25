using Management_System_WPF.Models;
using System.Data;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Management_System_WPF.Helpers
{
    public static class PivotHelper
    {
        public static DataTable CreatePivotTableWithTotals(List<SalesRaw> raw)
        {
            DataTable dt = new DataTable();

            // ✅ NO DATA SAFETY
            if (raw == null || raw.Count == 0)
            {
                dt.Columns.Add("Date", typeof(string));
                DataRow row = dt.NewRow();
                row["Date"] = "No sales found";
                dt.Rows.Add(row);
                return dt;
            }

            var items = raw
                .Select(x => x.ItemName)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            dt.Columns.Add("Date", typeof(string));

            foreach (var item in items)
                dt.Columns.Add(item, typeof(decimal));

            // ---------------- NORMAL SALES ROWS ----------------
            var grouped = raw.GroupBy(x => x.Date);

            foreach (var group in grouped)
            {
                DataRow row = dt.NewRow();

                if (DateTime.TryParse(group.Key, out DateTime date))
                    row["Date"] = date.ToString("dd-MM-yyyy");
                else
                    row["Date"] = group.Key;

                foreach (var item in items)
                {
                    int qty = group.Where(x => x.ItemName == item).Sum(x => x.Qty);
                    row[item] = qty == 0 ? DBNull.Value : qty;
                }

                dt.Rows.Add(row);
            }

            // ---------------- TOTAL QTY ----------------
            DataRow totalQtyRow = dt.NewRow();
            totalQtyRow["Date"] = "Total Qty";

            foreach (var item in items)
            {
                int total = raw.Where(x => x.ItemName == item).Sum(x => x.Qty);
                totalQtyRow[item] = total == 0 ? DBNull.Value : total;
            }

            dt.Rows.Add(totalQtyRow);

            // ---------------- UNIT PRICE ----------------
            DataRow unitPriceRow = dt.NewRow();
            unitPriceRow["Date"] = "Unit Price";

            foreach (var item in items)
                unitPriceRow[item] = raw.First(x => x.ItemName == item).Price;

            dt.Rows.Add(unitPriceRow);

            // ---------------- TOTAL PRICE ----------------
            DataRow totalPriceRow = dt.NewRow();
            totalPriceRow["Date"] = "Total Price";

            foreach (var item in items)
            {
                if (totalQtyRow[item] == DBNull.Value)
                    totalPriceRow[item] = DBNull.Value;
                else
                    totalPriceRow[item] =
                        Convert.ToDecimal(totalQtyRow[item]) *
                        Convert.ToDecimal(unitPriceRow[item]);
            }

            dt.Rows.Add(totalPriceRow);

            // ---------------- GRAND TOTAL ----------------
            DataRow grandTotalRow = dt.NewRow();
            grandTotalRow["Date"] = "Grand Total";

            decimal grandTotal = 0;

            foreach (var item in items)
            {
                if (totalPriceRow[item] != DBNull.Value)
                    grandTotal += Convert.ToDecimal(totalPriceRow[item]);
            }

            // ✅ SAFE ASSIGNMENT
            if (items.Count > 0)
            {
                grandTotalRow[items[0]] = grandTotal;

                for (int i = 1; i < items.Count; i++)
                    grandTotalRow[items[i]] = DBNull.Value;
            }

            dt.Rows.Add(grandTotalRow);

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

            // 🔹 GRAND TOTAL ROW
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

            // DISTINCT ITEMS
            var items = raw.Select(x => x.ItemName).Distinct().OrderBy(x => x).ToList();

            // COLUMNS
            dt.Columns.Add("Date", typeof(string));
            foreach (var item in items)
                dt.Columns.Add(item, typeof(int));

            dt.Columns.Add("Total", typeof(int)); // ✅ ROW TOTAL COLUMN

            // GROUP BY DATE
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

            // ================= TOTAL ROW =================
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

            totalRow["Total"] = grandTotal; // ✅ GRAND TOTAL
            dt.Rows.Add(totalRow);

            return dt;
        }


    }
}
