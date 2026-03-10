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
        public static void AddReturn(int buyerId, int itemId, int year, int month, int qty)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string ym = $"{year}-{month:D2}";
            string sql = @"INSERT INTO returns (buyer_id, item_id, return_month, qty) 
                           VALUES (@b, @i, @ym, @q)
                           ON CONFLICT(return_id) DO UPDATE SET qty = qty + @q; -- Simplification
                           ";
            sql = "INSERT INTO returns (buyer_id, item_id, return_month, qty) VALUES (@b, @i, @ym, @q)";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@b", buyerId);
            cmd.Parameters.AddWithValue("@i", itemId);
            cmd.Parameters.AddWithValue("@ym", ym);
            cmd.Parameters.AddWithValue("@q", qty);
            cmd.ExecuteNonQuery();
        }
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
                    Date = "Return" 
                });
            }
            return list;
        }
        public static List<ReturnModel> GetDetailedReturns(int buyerId, int year, int month)
        {
            var list = new List<ReturnModel>();
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string ym = $"{year}-{month:D2}";

            string sql = @"SELECT r.return_id, r.item_id, r.qty, i.item_name 
                   FROM returns r
                   JOIN items i ON r.item_id = i.item_id
                   WHERE r.buyer_id = @b AND r.return_month = @ym";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@b", buyerId);
            cmd.Parameters.AddWithValue("@ym", ym);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ReturnModel
                {
                    ReturnId = Convert.ToInt32(reader["return_id"]),
                    ItemId = Convert.ToInt32(reader["item_id"]),
                    ItemName = reader["item_name"].ToString(),
                    Qty = Convert.ToInt32(reader["qty"])
                });
            }
            return list;
        }
        public static void UpdateReturn(int returnId, int newItemId, int newQty)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string sql = "UPDATE returns SET item_id = @iid, qty = @q WHERE return_id = @rid";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@iid", newItemId);
            cmd.Parameters.AddWithValue("@q", newQty);
            cmd.Parameters.AddWithValue("@rid", returnId);
            cmd.ExecuteNonQuery();
        }
        public static void DeleteReturn(int returnId)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM returns WHERE return_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", returnId);
            cmd.ExecuteNonQuery();
        }
    }
}