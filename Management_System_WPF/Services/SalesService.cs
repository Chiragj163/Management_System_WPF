using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Management_System_WPF.Models;
using System.Windows;


namespace Management_System_WPF.Services
{
    public static class SalesService
    {
        private static string connectionString =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

        // ================================
        // 1️⃣ CREATE SALE (returns sale_id)
        // ================================
        public static int CreateSale(int buyerId, DateTime date, decimal totalAmount)
        {
            using var conn = new SQLiteConnection(connectionString);
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
            using var conn = new SQLiteConnection(connectionString);
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

        // =================================
        // 3️⃣ GET ALL SALES (JOIN TABLES)
        // =================================
        public static List<SaleRecord> GetAllSaleRecords()
        {
            List<SaleRecord> list = new List<SaleRecord>();

            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string query = @"
SELECT 
    s.sale_id,
    b.buyer_name,
    i.item_name,
    si.qty,
    (si.qty * si.price) AS amount,
    s.sale_date
FROM sale_items si
JOIN sales s ON si.sale_id = s.sale_id
JOIN buyers b ON s.buyer_id = b.buyer_id
JOIN items i ON si.item_id = i.item_id
ORDER BY s.sale_id DESC;";


            using var cmd = new SQLiteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new SaleRecord
                {
                    SaleId = reader.GetInt32(0),
                    BuyerName = reader.GetString(1),
                    ItemName = reader.GetString(2),
                    Quantity = reader.GetInt32(3),
                    Amount = reader.GetDecimal(4),
                    SaleDate = reader.GetString(5)
                });
            }

            return list;
        }

        // =================================
        // 4️⃣ DELETE A SALE ITEM
        // =================================
        public static void DeleteSaleItem(int saleItemId)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string query = "DELETE FROM sale_items WHERE sale_item_id=@id";

            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", saleItemId);
            cmd.ExecuteNonQuery();
        }

        // =================================
        // 5️⃣ DELETE WHOLE SALE (and its items)
        // =================================

        public static void DeleteSale(int saleId)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            // Delete sale items
            using (var cmd1 = new SQLiteCommand("DELETE FROM sale_items WHERE sale_id = @id", conn))
            {
                cmd1.Parameters.AddWithValue("@id", saleId);
                cmd1.ExecuteNonQuery();
            }

            // Delete sale record
            using (var cmd2 = new SQLiteCommand("DELETE FROM sales WHERE sale_id = @id", conn))
            {
                cmd2.Parameters.AddWithValue("@id", saleId);
                cmd2.ExecuteNonQuery();
            }
        }

       


    }
}
