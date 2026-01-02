using System;
using System.Data.SQLite;
using System.IO;

namespace Management_System_WPF.Services
{
    public static class PaymentService
    {
        private static string connectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

        // ✅ Create table if it doesn't exist
        public static void Initialize()
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string sql = @"CREATE TABLE IF NOT EXISTS buyer_payments (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            buyer_id INTEGER,
                            payment_month TEXT, -- Format YYYY-MM
                            amount DECIMAL(10,2)
                          )";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        public static decimal GetPayment(int buyerId, int year, int month)
        {
            try
            {
                using var conn = new SQLiteConnection(connectionString);
                conn.Open();
                string ym = $"{year}-{month:D2}";
                string sql = "SELECT amount FROM buyer_payments WHERE buyer_id = @b AND payment_month = @ym";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@b", buyerId);
                cmd.Parameters.AddWithValue("@ym", ym);

                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception)
            {
                // If table doesn't exist or DB error, return 0 to prevent crash
                return 0;
            }
        }

        public static void SavePayment(int buyerId, int year, int month, decimal amount)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string ym = $"{year}-{month:D2}";

            // Check if record exists
            string checkSql = "SELECT id FROM buyer_payments WHERE buyer_id=@b AND payment_month=@ym";
            using var checkCmd = new SQLiteCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@b", buyerId);
            checkCmd.Parameters.AddWithValue("@ym", ym);
            var id = checkCmd.ExecuteScalar();

            string sql;
            if (id != null)
                sql = "UPDATE buyer_payments SET amount = @a WHERE id = @id";
            else
                sql = "INSERT INTO buyer_payments (buyer_id, payment_month, amount) VALUES (@b, @ym, @a)";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@b", buyerId);
            cmd.Parameters.AddWithValue("@ym", ym);
            cmd.Parameters.AddWithValue("@a", amount);
            if (id != null) cmd.Parameters.AddWithValue("@id", id);

            cmd.ExecuteNonQuery();
        }
    }
}