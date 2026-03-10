using Management_System_WPF.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Management_System_WPF.Services
{
    public static class ItemsService
    {
        private static string connectionString =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";
        public static void AddItem(Item item)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

              
                string query = "INSERT INTO items (item_name, price, category) VALUES (@name, @price, @category)";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", item.Name);
                    cmd.Parameters.AddWithValue("@price", item.Price);
                    cmd.Parameters.AddWithValue("@category", item.Category ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static List<Item> GetAllItems()
        {
            List<Item> items = new List<Item>();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT item_id, item_name, price, category FROM items";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new Item
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Price = reader.GetDecimal(2),
                           
                            Category = reader.IsDBNull(3) ? "" : reader.GetString(3)
                        });
                    }
                }
            }

            return items;
        }

        public static void UpdateItem(Item item)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE items SET item_name = @name, price = @price, category = @category WHERE item_id = @id";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", item.Name);
                    cmd.Parameters.AddWithValue("@price", item.Price);
                    cmd.Parameters.AddWithValue("@category", item.Category ?? "");
                    cmd.Parameters.AddWithValue("@id", item.Id);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteItem(int id)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string query = "DELETE FROM items WHERE item_id = @id";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}