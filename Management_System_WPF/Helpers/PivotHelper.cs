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

            var items = raw.Select(x => x.ItemName).Distinct().ToList();

            dt.Columns.Add("Date", typeof(string));

            // Use decimal for safety (qty / price / total)
            foreach (var item in items)
                dt.Columns.Add(item, typeof(decimal));

            // -------------------- NORMAL SALES ROWS --------------------
            var grouped = raw.GroupBy(x => x.Date);

            foreach (var group in grouped)
            {
                DataRow row = dt.NewRow();
                DateTime date;

                if (DateTime.TryParse(group.Key.ToString(), out date))
                    row["Date"] = date.ToString("dd-MM-yyyy");
                else
                    row["Date"] = group.Key.ToString();

                foreach (var item in items)
                {
                    int qty = group.Where(x => x.ItemName == item).Sum(x => x.Qty);
                    row[item] = qty == 0 ? DBNull.Value : qty;
                }

                dt.Rows.Add(row);
            }

            // -------------------- TOTAL QTY --------------------
            DataRow totalQtyRow = dt.NewRow();
            totalQtyRow["Date"] = "Total Qty";

            foreach (var item in items)
            {
                int total = raw.Where(x => x.ItemName == item).Sum(x => x.Qty);
                totalQtyRow[item] = total == 0 ? DBNull.Value : total;
            }

            dt.Rows.Add(totalQtyRow);

            // -------------------- UNIT PRICE --------------------
            DataRow unitPriceRow = dt.NewRow();
            unitPriceRow["Date"] = "Unit Price";

            foreach (var item in items)
                unitPriceRow[item] = raw.FirstOrDefault(x => x.ItemName == item)?.Price ?? 0;

            dt.Rows.Add(unitPriceRow);

            // -------------------- TOTAL PRICE --------------------
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

            // -------------------- GRAND TOTAL (ROW AFTER TOTAL PRICE) --------------------
            DataRow grandTotalRow = dt.NewRow();
            grandTotalRow["Date"] = "Grand Total";

            decimal grandTotal = 0;

            foreach (var item in items)
            {
                if (totalPriceRow[item] != DBNull.Value)
                    grandTotal += Convert.ToDecimal(totalPriceRow[item]);
            }

            // show grand total only once (first column)
            grandTotalRow[items[0]] = grandTotal;

            // empty remaining columns
            for (int i = 1; i < items.Count; i++)
                grandTotalRow[items[i]] = DBNull.Value;

            dt.Rows.Add(grandTotalRow);

            return dt;
        }


    }
}
