using Management_System_WPF.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Management_System_WPF.Services
{
    public static class PaymentService
    {
        private static string connectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

        // 1. INITIALIZE TABLE
        public static void Initialize()
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            string sql = @"CREATE TABLE IF NOT EXISTS buyer_payments (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            buyer_id INTEGER,
                            payment_date TEXT, -- Format YYYY-MM-DD
                            amount DECIMAL(10,2)
                          )";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // 2. GET TOTAL MONTHLY PAYMENT (This was missing!)
        public static decimal GetPayment(int buyerId, int year, int month)
        {
            try
            {
                using var conn = new SQLiteConnection(connectionString);
                conn.Open();

                // Select the SUM of all payments for this specific YYYY-MM
                string sql = @"SELECT IFNULL(SUM(amount), 0) 
                               FROM buyer_payments 
                               WHERE buyer_id = @b 
                               AND strftime('%Y', payment_date) = @y 
                               AND strftime('%m', payment_date) = @m";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@b", buyerId);
                cmd.Parameters.AddWithValue("@y", year.ToString());
                cmd.Parameters.AddWithValue("@m", month.ToString("00"));

                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // 3. ADD NEW PAYMENT
        public static void AddPayment(int buyerId, DateTime date, decimal amount)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string sql = "INSERT INTO buyer_payments (buyer_id, payment_date, amount) VALUES (@b, @date, @a)";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@b", buyerId);
            cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@a", amount);
            cmd.ExecuteNonQuery();
        }

        // 4. UPDATE EXISTING PAYMENT
        public static void UpdatePayment(int paymentId, DateTime date, decimal amount)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string sql = "UPDATE buyer_payments SET payment_date = @date, amount = @a WHERE id = @id";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", paymentId);
            cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@a", amount);
            cmd.ExecuteNonQuery();
        }

        // 5. DELETE PAYMENT
        public static void DeletePayment(int paymentId)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();
            string sql = "DELETE FROM buyer_payments WHERE id = @id";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", paymentId);
            cmd.ExecuteNonQuery();
        }

        // 6. GET PAYMENT HISTORY LIST (For the grid)
        public static List<PaymentRecord> GetPaymentsList(int buyerId, int year, int month)
        {
            var list = new List<PaymentRecord>();
            try
            {
                using var conn = new SQLiteConnection(connectionString);
                conn.Open();
                string sql = @"SELECT id, payment_date, amount 
                               FROM buyer_payments 
                               WHERE buyer_id = @b 
                               AND strftime('%Y', payment_date) = @y 
                               AND strftime('%m', payment_date) = @m
                               ORDER BY payment_date DESC";

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@b", buyerId);
                cmd.Parameters.AddWithValue("@y", year.ToString());
                cmd.Parameters.AddWithValue("@m", month.ToString("00"));

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PaymentRecord
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Date = DateTime.Parse(reader["payment_date"].ToString()),
                        Amount = Convert.ToDecimal(reader["amount"])
                    });
                }
            }
            catch { }
            return list;
        }
    }
}