using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Management_System_WPF.Models;

namespace Management_System_WPF.Services
{
    public static class ReturnService
    {
        private static string connectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

        public static void Initialize()
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string sql = @"CREATE TABLE IF NOT EXISTS returns (
                            return_id INTEGER PRIMARY KEY AUTOINCREMENT,
                            buyer_id INTEGER,
                            item_id INTEGER,
                            return_month TEXT, -- Format YYYY-MM
                            qty INTEGER
                          )";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // Save a return
        public static void AddReturn(int buyerId, int itemId, int year, int month, int qty)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string ym = $"{year}-{month:D2}";

            // Check if return exists for this item in this month, update it or insert new
            string sql = @"INSERT INTO returns (buyer_id, item_id, return_month, qty) 
                           VALUES (@b, @i, @ym, @q)
                           ON CONFLICT(return_id) DO UPDATE SET qty = qty + @q; -- Simplification
                           ";

            // Note: For simplicity, we just insert. You might want to grouping logic in SQL, 
            // but here we will just Insert and Sum later.
            sql = "INSERT INTO returns (buyer_id, item_id, return_month, qty) VALUES (@b, @i, @ym, @q)";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@b", buyerId);
            cmd.Parameters.AddWithValue("@i", itemId);
            cmd.Parameters.AddWithValue("@ym", ym);
            cmd.Parameters.AddWithValue("@q", qty);
            cmd.ExecuteNonQuery();
        }

        // Get returns for a specific month
        public static List<SalesRaw> GetReturnsForPivot(int buyerId, int year, int month)
        {
            var list = new List<SalesRaw>();
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string ym = $"{year}-{month:D2}";

            string sql = @"SELECT r.item_id, i.item_name, SUM(r.qty) as qty
                           FROM returns r
                           JOIN items i ON r.item_id = i.item_id
                           WHERE r.buyer_id = @b AND r.return_month = @ym
                           GROUP BY r.item_id, i.item_name";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@b", buyerId);
            cmd.Parameters.AddWithValue("@ym", ym);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SalesRaw
                {
                    ItemName = reader["item_name"].ToString(),
                    Qty = Convert.ToInt32(reader["qty"]),
                    Date = "Return" // Marker
                });
            }
            return list;
        }
    }
}