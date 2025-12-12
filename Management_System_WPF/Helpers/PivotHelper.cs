using Management_System_WPF.Models;
using System.Data;
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

            foreach (var item in items)
                dt.Columns.Add(item, typeof(int));

            // -------------------- NORMAL SALES ROWS --------------------
            var grouped = raw.GroupBy(x => x.Date);

            foreach (var group in grouped)
            {
                DataRow row = dt.NewRow();
                row["Date"] = group.Key;

                foreach (var item in items)
                {
                    row[item] = group.Where(x => x.ItemName == item).Sum(x => x.Qty);
                }

                dt.Rows.Add(row);
            }

            // -------------------- TOTAL QTY ROW --------------------
            DataRow totalQty = dt.NewRow();
            totalQty["Date"] = "Total Qty";

            foreach (var item in items)
            {
                totalQty[item] = dt.AsEnumerable()
                                    .Where(r => r["Date"].ToString() != "Total Qty")
                                    .Sum(r => r.Field<int>(item));
            }

            dt.Rows.Add(totalQty);

            // -------------------- UNIT PRICE ROW --------------------
            DataRow unitPrice = dt.NewRow();
            unitPrice["Date"] = "Unit Price";

            foreach (var item in items)
            {
                unitPrice[item] = raw
                    .Where(x => x.ItemName == item)
                    .Select(x => x.Price)
                    .FirstOrDefault();
            }

            dt.Rows.Add(unitPrice);

            // -------------------- TOTAL PRICE ROW --------------------
            DataRow totalPrice = dt.NewRow();
            totalPrice["Date"] = "Total Price";

            foreach (var item in items)
            {
                int qty = Convert.ToInt32(totalQty[item]);
                decimal price = Convert.ToDecimal(unitPrice[item]);
                totalPrice[item] = qty * price;
            }

            dt.Rows.Add(totalPrice);

            return dt;
        }
    }
}
