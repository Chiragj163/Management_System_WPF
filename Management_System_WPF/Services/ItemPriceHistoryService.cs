using Management_System_WPF.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Management_System_WPF.Services
{
    public static class ItemPriceHistoryService
    {
        private static string connectionString =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

        // ---------------- ADD HISTORY ----------------
        public static void AddHistory(ItemPriceHistory history)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string query = @"
                    INSERT INTO item_price_history
                    (item_id, old_price, new_price, changed_on)
                    VALUES (@itemId, @oldPrice, @newPrice, @changedOn)";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@itemId", history.ItemId);
                    cmd.Parameters.AddWithValue("@oldPrice", history.OldPrice);
                    cmd.Parameters.AddWithValue("@newPrice", history.NewPrice);
                    cmd.Parameters.AddWithValue("@changedOn", history.ChangedOn);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ---------------- GET HISTORY ----------------
        public static List<ItemPriceHistory> GetHistory(int itemId)
        {
            var list = new List<ItemPriceHistory>();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string query = @"
                    SELECT id, old_price, new_price, changed_on
                    FROM item_price_history
                    WHERE item_id = @itemId
                    ORDER BY changed_on DESC";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@itemId", itemId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ItemPriceHistory
                            {
                                Id = reader.GetInt32(0),
                                ItemId = itemId,
                                OldPrice = reader.GetDecimal(1),
                                NewPrice = reader.GetDecimal(2),
                                ChangedOn = reader.GetDateTime(3)
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}
