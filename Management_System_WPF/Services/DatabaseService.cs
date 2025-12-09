using Management_System_WPF.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Management_System.Services
{
    internal static class DatabaseService
    {
        public static SQLiteConnection GetConnection()
        {
            string dbPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Database",
                "factory.db");

            return new SQLiteConnection($"Data Source={dbPath};Version=3;");
        }

        // 🔥 Correct version that MATCHES your real tables
        public static List<SaleRecord> GetSales()
        {
            var list = new List<SaleRecord>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    s.sale_id,
                    s.buyer_id,
                    b.name AS buyer_name,
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
    }
}
