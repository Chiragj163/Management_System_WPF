using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Management_System_WPF.Models;

namespace Management_System_WPF.Services
{
    public static class BuyersService
    {
        private static string connectionString =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

        public static void AddBuyer(Buyer buyer)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string query = "INSERT INTO buyers (buyer_name, phone) VALUES (@name, @phone)";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", buyer.Name);
                    cmd.Parameters.AddWithValue("@phone", buyer.Phone);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void UpdateBuyer(Buyer buyer)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE buyers SET buyer_name=@name WHERE buyer_id=@id";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", buyer.Name);
                    cmd.Parameters.AddWithValue("@id", buyer.BuyerId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteBuyer(int id)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM buyers WHERE buyer_id=@id";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static List<Buyer> GetAllBuyers()
        {
            var buyers = new List<Buyer>();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string query = "SELECT buyer_id, buyer_name, phone FROM buyers";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        buyers.Add(new Buyer
                        {
                            BuyerId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Phone = reader.GetString(2)
                        });
                    }
                }
            }

            return buyers;
        }
    }
}
