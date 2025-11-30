using Management_System_WPF.Models;
using System.Collections.Generic;
using System.Data.SQLite;

public static class ItemsService
{
    private static string connectionString =
        $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

    // ---------------------- ADD ITEM ----------------------
    public static void AddItem(Item item)
    {
        using (var conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            string query = "INSERT INTO items (item_name, price) VALUES (@name, @price)";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@price", item.Price);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ---------------------- GET ALL ITEMS ----------------------
    public static List<Item> GetAllItems()
    {
        List<Item> items = new List<Item>();

        using (var conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            string query = "SELECT item_id, item_name, price FROM items";

            using (var cmd = new SQLiteCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new Item
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Price = reader.GetDecimal(2)
                    });
                }
            }
        }

        return items;
    }


    // ---------------------- UPDATE ITEM ----------------------
    public static void UpdateItem(Item item)
    {
        using (var conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            string query = "UPDATE items SET item_name = @name, price = @price WHERE item_id = @id";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@price", item.Price);
                cmd.Parameters.AddWithValue("@id", item.Id);

                cmd.ExecuteNonQuery();
            }
        }
    }


    // ---------------------- DELETE ITEM ----------------------
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
