using Management_System_WPF.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Data;


namespace Management_System_WPF.Services
{
    public static class SalesService
    {

        private static string connectionString =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

        private static SQLiteConnection GetConnection() => new SQLiteConnection(connectionString);

        // ================================
        // 1️⃣ CREATE SALE
        // ================================
        public static int CreateSale(int buyerId, DateTime date, decimal totalAmount)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = @"
                INSERT INTO sales (buyer_id, sale_date, total_amount)
                VALUES (@buyer, @date, @amount);
                SELECT last_insert_rowid();";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@buyer", buyerId);
            cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@amount", totalAmount);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // =================================
        // 2️⃣ ADD SALE ITEM
        // =================================
        public static void AddSaleItem(int saleId, int itemId, int qty, decimal price)
        {
            using var conn = GetConnection();
            conn.Open();

            string query =
                "INSERT INTO sale_items (sale_id, item_id, qty, price) VALUES (@sale, @item, @qty, @price)";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@sale", saleId);
            cmd.Parameters.AddWithValue("@item", itemId);
            cmd.Parameters.AddWithValue("@qty", qty);
            cmd.Parameters.AddWithValue("@price", price);

            cmd.ExecuteNonQuery();
        }
        private static DateTime NormalizeMonth(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }


        // =================================
        // 3️⃣ GET ALL SALES RECORDS
        // =================================
        public static List<SaleRecord> GetAllSaleRecords()
        {
            var list = new List<SaleRecord>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
    SELECT 
    si.sale_item_id,
    s.sale_id,
    b.buyer_id,
    b.buyer_name,
    i.item_id,
    i.item_name,
    si.qty,
    si.price,
    (si.qty * si.price) AS amount,
    s.sale_date
FROM sale_items si
JOIN sales s ON si.sale_id = s.sale_id
JOIN buyers b ON s.buyer_id = b.buyer_id
JOIN items i ON si.item_id = i.item_id
ORDER BY s.sale_id DESC;

";


            using var cmd = new SQLiteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new SaleRecord
                {
                    SaleItemId = reader.GetInt32(0),
                    SaleId = reader.GetInt32(1),
                    BuyerId = reader.GetInt32(2),
                    BuyerName = reader.GetString(3),
                    ItemId = reader.GetInt32(4),
                    ItemName = reader.GetString(5),
                    Qty = reader.GetInt32(6),
                    Price = reader.GetDouble(7),
                    Amount = reader.GetDouble(8),
                    SaleDate = DateTime.Parse(reader.GetString(9))
                });

            }

            return list;
        }

        // =================================
        // 4️⃣ GET SALES BY BUYER (JOIN)
        // =================================
        public static List<SaleRecord> GetSales()
        {
            var list = new List<SaleRecord>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
    SELECT 
        s.sale_id,
        s.buyer_id,
        b.buyer_name AS buyer_name,
        s.sale_date,
        s.total_amount,
        si.item_id,
        i.item_name,
        si.qty,
        si.price
    FROM sales s
    LEFT JOIN sale_items si ON s.sale_id = si.sale_id
    LEFT JOIN items i ON si.item_id = i.item_id
    LEFT JOIN buyers b ON s.buyer_id = b.buyer_id;
";


            using var cmd = new SQLiteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new SaleRecord
                {
                    SaleId = reader.GetInt32(0),
                    BuyerId = reader.GetInt32(1),
                    BuyerName = reader.GetString(2),

                    SaleDate = DateTime.Parse(reader.GetString(3)),
                    TotalAmount = reader.GetDouble(4),

                    ItemId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    ItemName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Qty = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    Price = reader.IsDBNull(8) ? 0 : reader.GetDouble(8)
                });
            }

            return list;
        }
        public static List<SaleByBuyerRow> GetBuyerMatrix(int year, int month)
        {
            var rows = new Dictionary<DateTime, SaleByBuyerRow>();

            using var conn = GetConnection();
            conn.Open();

            string ym = new DateTime(year, month, 1).ToString("yyyy-MM");

            string query = @"
        SELECT 
            s.sale_date,           -- TEXT 'yyyy-MM-dd'
            b.buyer_name,         -- buyer name
            SUM(si.qty * si.price) AS total
        FROM sales s
        JOIN buyers b     ON b.buyer_id = s.buyer_id
        JOIN sale_items si ON si.sale_id = s.sale_id
        WHERE strftime('%Y-%m', s.sale_date) = @ym
        GROUP BY s.sale_date, b.buyer_name
        ORDER BY s.sale_date, b.buyer_name;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@ym", ym);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // sale_date stored as TEXT in DB, so parse to DateTime
                var date = DateTime.Parse(reader.GetString(0));
                var buyerName = reader.GetString(1);
                var total = Convert.ToDouble(reader["total"]);

                if (!rows.TryGetValue(date, out var row))
                {
                    row = new SaleByBuyerRow { Date = date };
                    rows[date] = row;
                }

                row.BuyerValues[buyerName] = total;
            }

            return rows.Values.OrderBy(r => r.Date).ToList();
        }



        public static List<SaleRecord> GetSalesByBuyer(string buyerName)
        {
            return GetSales()
                .Where(r => r.BuyerName == buyerName)
                .ToList();
        }

        // =================================
        // 5️⃣ GET BUYER DETAIL REPORT
        // =================================
        public static List<BuyerSaleRecord> GetSalesByBuyer(int buyerId)
        {
            var list = new List<BuyerSaleRecord>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    s.sale_date,
                    i.item_name,
                    si.qty,
                    si.price,
                    (si.qty * si.price) AS total
                FROM sales s
                INNER JOIN sale_items si ON s.sale_id = si.sale_id
                INNER JOIN items i ON si.item_id = i.item_id
                WHERE s.buyer_id = @buyerId
                ORDER BY s.sale_date ASC;
            ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@buyerId", buyerId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BuyerSaleRecord
                {
                    SaleDate = reader["sale_date"].ToString(),
                    ItemName = reader["item_name"].ToString(),
                    Quantity = Convert.ToInt32(reader["qty"]),
                    Price = Convert.ToDouble(reader["price"]),
                    Total = Convert.ToDouble(reader["total"])
                });
            }

            return list;
        }

        public static List<BuyerSaleRecord> GetPreviousMonthSales(int buyerId)
        {
            var list = new List<BuyerSaleRecord>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    s.sale_date,
                    i.item_name,
                    si.qty,
                    si.price,
                    (si.qty * si.price) AS total
                FROM sales s
                JOIN sale_items si ON s.sale_id = si.sale_id
                JOIN items i ON si.item_id = i.item_id
                WHERE s.buyer_id = @buyerId
                  AND strftime('%Y-%m', s.sale_date) = strftime('%Y-%m', 'now', '-1 month')
                ORDER BY s.sale_date;
            ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@buyerId", buyerId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BuyerSaleRecord
                {
                    SaleDate = reader["sale_date"].ToString(),
                    ItemName = reader["item_name"].ToString(),
                    Quantity = Convert.ToInt32(reader["qty"]),
                    Price = Convert.ToDouble(reader["price"]),
                    Total = Convert.ToDouble(reader["total"])
                });
            }

            return list;
        }

        // =================================
        // DELETE FUNCTIONS
        // =================================
        public static void DeleteSaleItem(int saleItemId)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = "DELETE FROM sale_items WHERE sale_item_id=@id";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", saleItemId);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteSale(int saleId)
        {
            using var conn = GetConnection();
            conn.Open();

            new SQLiteCommand("DELETE FROM sale_items WHERE sale_id=@id", conn)
            { Parameters = { new SQLiteParameter("@id", saleId) } }
            .ExecuteNonQuery();

            new SQLiteCommand("DELETE FROM sales WHERE sale_id=@id", conn)
            { Parameters = { new SQLiteParameter("@id", saleId) } }
            .ExecuteNonQuery();
        }

        // =================================
        // ARTICLE-SALES REPORT
        // =================================
        public static List<(string Date, string Article, double Total)> GetArticleSales()
        {
            var list = new List<(string, string, double)>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    s.sale_date,
                    i.item_name,
                    (si.qty * si.price) AS total
                FROM sales s
                JOIN sale_items si ON s.sale_id = si.sale_id
                JOIN items i ON si.item_id = i.item_id
                ORDER BY s.sale_date;
            ";

            using var cmd = new SQLiteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add((
                    reader["sale_date"].ToString(),
                    reader["item_name"].ToString(),
                    Convert.ToDouble(reader["total"])
                ));
            }

            return list;
        }

        public static List<(string Date, string Article, double Total)> GetArticleSalesPreviousMonth()
        {
            var list = new List<(string, string, double)>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    s.sale_date,
                    i.item_name,
                    (si.qty * si.price) AS total
                FROM sales s
                JOIN sale_items si ON s.sale_id = si.sale_id
                JOIN items i ON si.item_id = i.item_id
                WHERE strftime('%Y-%m', s.sale_date) = strftime('%Y-%m', 'now', '-1 month')
                ORDER BY s.sale_date;
            ";

            using var cmd = new SQLiteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add((
                    reader["sale_date"].ToString(),
                    reader["item_name"].ToString(),
                    Convert.ToDouble(reader["total"])
                ));
            }

            return list;
        }
        // ======================================================
        //  🔵 GET RAW SALES DATA FOR PIVOT REPORT (DYNAMIC ITEMS)
        // ======================================================
        public static List<SalesRaw> GetSalesRawForPivot(int buyerId, int year, int month)
        {
            var list = new List<SalesRaw>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT 
            s.sale_date,
            i.item_name,
            si.qty,
            si.price
        FROM sale_items si
        JOIN items i ON si.item_id = i.item_id
        JOIN sales s ON si.sale_id = s.sale_id
        WHERE s.buyer_id = @buyerId
          AND strftime('%Y-%m', s.sale_date) = @ym
        ORDER BY s.sale_date;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@buyerId", buyerId);

            string ym = $"{year}-{month:D2}";
            cmd.Parameters.AddWithValue("@ym", ym);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SalesRaw
                {
                    Date = reader.GetString(0),
                    ItemName = reader.GetString(1),
                    Qty = Convert.ToInt32(reader["qty"]),
                    Price = Convert.ToDecimal(reader["price"])
                });
            }

            return list;
        }
        public static bool HasSalesInMonth(int buyerId, int year, int month)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string query = @"
        SELECT COUNT(*)
        FROM sales s
        JOIN sale_items si ON s.sale_id = si.sale_id
        WHERE s.buyer_id = @buyerId
          AND strftime('%Y-%m', s.sale_date) = @ym
    ";

            string ym = $"{year}-{month:D2}";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@buyerId", buyerId);
            cmd.Parameters.AddWithValue("@ym", ym);

            long count = (long)cmd.ExecuteScalar();
            return count > 0;
        }
        public static List<SalesRaw> GetSalesBetweenDates(int buyerId, DateTime from, DateTime to)
        {
            var list = new List<SalesRaw>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT 
            s.sale_date,
            i.item_name,
            si.qty,
            si.price
        FROM sale_items si
        JOIN items i ON si.item_id = i.item_id
        JOIN sales s ON s.sale_id = si.sale_id
        WHERE s.buyer_id = @buyerId
          AND date(s.sale_date) BETWEEN date(@from) AND date(@to)
        ORDER BY s.sale_date;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@buyerId", buyerId);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SalesRaw
                {
                    Date = reader.GetString(0),
                    ItemName = reader.GetString(1),
                    Qty = Convert.ToInt32(reader["qty"]),
                    Price = Convert.ToDecimal(reader["price"])
                });
            }

            return list;
        }
        public static List<(string Date, string Article, int Qty)> GetArticleSalesByMonth(int year, int month)
        {
            var list = new List<(string, string, int)>();

            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string query = @"
        SELECT 
            s.sale_date,
            i.item_name,
            si.qty             -- ✅ THIS is what we need!
        FROM sales s
        JOIN sale_items si ON s.sale_id = si.sale_id
        JOIN items i ON si.item_id = i.item_id
        WHERE strftime('%Y', s.sale_date) = @year
          AND strftime('%m', s.sale_date) = @month
        ORDER BY s.sale_date;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@year", year.ToString());
            cmd.Parameters.AddWithValue("@month", month.ToString("D2"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add((
                    reader["sale_date"].ToString(),
                    reader["item_name"].ToString(),
                    Convert.ToInt32(reader["qty"])
                ));
            }

            return list;
        }
        public static bool HasSalesInMonth(int year, int month)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string query = @"
        SELECT COUNT(*)
        FROM sales
        WHERE strftime('%Y', sale_date) = @year
          AND strftime('%m', sale_date) = @month
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@year", year.ToString());
            cmd.Parameters.AddWithValue("@month", month.ToString("D2"));

            long count = (long)cmd.ExecuteScalar();
            return count > 0;
        }
        // ✅ Get the nearest previous month that has sales
        public static DateTime? GetPreviousSalesMonth(DateTime currentMonth)
        {
            currentMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);

            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT DISTINCT strftime('%Y-%m-01', sale_date)
        FROM sales
        WHERE date(sale_date) < date(@currentMonth)
        ORDER BY 1 DESC
        LIMIT 1;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@currentMonth", currentMonth.ToString("yyyy-MM-dd"));

            var result = cmd.ExecuteScalar();
            return result == null ? null : DateTime.Parse(result.ToString());
        }

        // ✅ Get the nearest next month that has sales
        public static DateTime? GetNextSalesMonth(DateTime currentMonth)
        {
            currentMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            DateTime nextMonthStart = currentMonth.AddMonths(1);

            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT DISTINCT strftime('%Y-%m-01', sale_date)
        FROM sales
        WHERE date(sale_date) >= date(@nextMonthStart)
        ORDER BY 1
        LIMIT 1;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@nextMonthStart", nextMonthStart.ToString("yyyy-MM-dd"));

            var result = cmd.ExecuteScalar();
            return result == null ? null : DateTime.Parse(result.ToString());
        }

        // ==========================================
        // 🔁 PREVIOUS SALES MONTH FOR BUYER
        // ==========================================
        public static DateTime? GetPreviousSalesMonthForBuyer(int buyerId, DateTime currentMonth)
        {
            currentMonth = NormalizeMonth(currentMonth);

            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT DISTINCT strftime('%Y-%m-01', sale_date)
        FROM sales
        WHERE buyer_id = @buyerId
          AND date(sale_date) < date(@currentMonth)
        ORDER BY 1 DESC
        LIMIT 1;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@buyerId", buyerId);
            cmd.Parameters.AddWithValue("@currentMonth", currentMonth.ToString("yyyy-MM-dd"));

            var result = cmd.ExecuteScalar();
            return result == null ? null : DateTime.Parse(result.ToString());
        }




        // ==========================================
        // 🔁 NEXT SALES MONTH FOR BUYER
        // ==========================================
        public static DateTime? GetNextSalesMonthForBuyer(int buyerId, DateTime currentMonth)
        {
            currentMonth = NormalizeMonth(currentMonth);
            DateTime nextMonthStart = currentMonth.AddMonths(1);

            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT DISTINCT strftime('%Y-%m-01', sale_date)
        FROM sales
        WHERE buyer_id = @buyerId
          AND date(sale_date) >= date(@nextMonthStart)
        ORDER BY 1
        LIMIT 1;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@buyerId", buyerId);
            cmd.Parameters.AddWithValue("@nextMonthStart", nextMonthStart.ToString("yyyy-MM-dd"));

            var result = cmd.ExecuteScalar();
            return result == null ? null : DateTime.Parse(result.ToString());
        }




        // ==========================================
        // 🔁 PREVIOUS MONTH WITH ARTICLE SALES
        // ==========================================
        public static DateTime? GetPreviousArticleSalesMonth(DateTime currentMonth)
        {
            currentMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);

            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT DISTINCT strftime('%Y-%m-01', sale_date)
        FROM sales
        WHERE date(sale_date) < date(@currentMonth)
        ORDER BY 1 DESC
        LIMIT 1;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@currentMonth", currentMonth.ToString("yyyy-MM-dd"));

            var result = cmd.ExecuteScalar();
            return result == null ? null : DateTime.Parse(result.ToString());
        }


        // ==========================================
        // 🔁 NEXT MONTH WITH ARTICLE SALES
        // ==========================================
        public static DateTime? GetNextArticleSalesMonth(DateTime currentMonth)
        {
            currentMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            DateTime nextMonthStart = currentMonth.AddMonths(1);

            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT DISTINCT strftime('%Y-%m-01', sale_date)
        FROM sales
        WHERE date(sale_date) >= date(@nextMonthStart)
        ORDER BY 1
        LIMIT 1;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@nextMonthStart", nextMonthStart.ToString("yyyy-MM-dd"));

            var result = cmd.ExecuteScalar();
            return result == null ? null : DateTime.Parse(result.ToString());
        }

        // ✅ SQLITE VERSION
        public static int GetArticleQtyByDate(string article, DateTime date)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT IFNULL(SUM(si.qty), 0)
        FROM sales s
        JOIN sale_items si ON s.sale_id = si.sale_id
        JOIN items i ON si.item_id = i.item_id
        WHERE i.item_name = @article
          AND date(s.sale_date) = date(@date);
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@article", article);
            cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdateArticleSaleQty(string article, DateTime date, int qty)
        {
            using var conn = GetConnection();
            conn.Open();

              string query = @"
                     UPDATE sale_items
                     SET qty = @qty
                     WHERE sale_id IN (
                     SELECT s.sale_id
                     FROM sales s
                     JOIN sale_items si ON s.sale_id = si.sale_id
                     JOIN items i ON si.item_id = i.item_id
                     WHERE i.item_name = @article
                    AND date(s.sale_date) = date(@date)
                             );
                    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@qty", qty);
            cmd.Parameters.AddWithValue("@article", article);
            cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            cmd.ExecuteNonQuery();
        }

    

    public static void UpdateSaleQty(int saleId, int itemId, int qty)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = @"
        UPDATE sale_items
        SET qty = @qty
        WHERE sale_id = @saleId AND item_id = @itemId;
    ";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@qty", qty);
            cmd.Parameters.AddWithValue("@saleId", saleId);
            cmd.Parameters.AddWithValue("@itemId", itemId);

            cmd.ExecuteNonQuery();
        }
        public static void UpdateSaleItemQty(int saleItemId, int qty)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new SQLiteCommand(
                "UPDATE sale_items SET qty = @qty WHERE sale_item_id = @id",
                conn);

            cmd.Parameters.AddWithValue("@qty", qty);
            cmd.Parameters.AddWithValue("@id", saleItemId);

            cmd.ExecuteNonQuery();
        }


        // =======================================
        // ✅ UPDATE FULL SALE ITEM (BUYER + ITEM + QTY)
        // =======================================
        public static void UpdateSaleItemFull(
            int saleItemId,
            int saleId,
            int buyerId,
            int itemId,
            int qty)
        {
            using var conn = GetConnection();
            conn.Open();

            using var tx = conn.BeginTransaction();

            // 🔵 Update buyer in SALES table
            new SQLiteCommand(
                "UPDATE sales SET buyer_id=@b WHERE sale_id=@s",
                conn, tx)
            {
                Parameters =
        {
            new SQLiteParameter("@b", buyerId),
            new SQLiteParameter("@s", saleId)
        }
            }.ExecuteNonQuery();

            // 🔵 Update item + qty in SALE_ITEMS table
            new SQLiteCommand(
                "UPDATE sale_items SET item_id=@i, qty=@q WHERE sale_item_id=@id",
                conn, tx)
            {
                Parameters =
        {
            new SQLiteParameter("@i", itemId),
            new SQLiteParameter("@q", qty),
            new SQLiteParameter("@id", saleItemId)
        }
            }.ExecuteNonQuery();

            tx.Commit();
        }




    }
}

