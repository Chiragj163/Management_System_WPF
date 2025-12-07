using System.Data.SQLite;

public static class SalesService
{
    private static string connectionString =
        $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

    public static int CreateSale(int buyerId, DateTime date)
    {
        using var conn = new SQLiteConnection(connectionString);
        conn.Open();

        string query = "INSERT INTO sales (buyer_id, sale_date) VALUES (@buyer, @date); SELECT last_insert_rowid();";

        using var cmd = new SQLiteCommand(query, conn);
        cmd.Parameters.AddWithValue("@buyer", buyerId);
        cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static void AddSaleItem(int saleId, int itemId, int qty, decimal price)
    {
        using var conn = new SQLiteConnection(connectionString);
        conn.Open();

        string query = "INSERT INTO sale_items (sale_id, item_id, quantity, price) VALUES (@sale, @item, @qty, @price)";

        using var cmd = new SQLiteCommand(query, conn);
        cmd.Parameters.AddWithValue("@sale", saleId);
        cmd.Parameters.AddWithValue("@item", itemId);
        cmd.Parameters.AddWithValue("@qty", qty);
        cmd.Parameters.AddWithValue("@price", price);

        cmd.ExecuteNonQuery();
    }
}
