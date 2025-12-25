using Management_System_WPF.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Management_System_WPF.Services
{
    public static class SpecialPriceService
    {
        private static string _conn =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "factory.db")};Version=3;";

        // =========================================================
        // 🔵 BUYER RELEVANT ARTICLES ONLY
        // Bought OR already has special price
        // =========================================================
        public static List<SpecialPriceVM> GetBuyerRelevantArticles(int buyerId)
        {
            var list = new List<SpecialPriceVM>();

            using var con = new SQLiteConnection(_conn);
            con.Open();

            string sql = @"
SELECT DISTINCT
    i.item_id,
    i.item_name,
    i.price AS OriginalPrice,
    COALESCE(sp.special_price, i.price) AS SpecialPrice
FROM items i

LEFT JOIN sale_items si ON si.item_id = i.item_id
LEFT JOIN sales s 
    ON s.sale_id = si.sale_id 
   AND s.buyer_id = @BuyerId

LEFT JOIN special_prices sp
    ON sp.item_id = i.item_id
   AND sp.buyer_id = @BuyerId

WHERE s.buyer_id IS NOT NULL
   OR sp.item_id IS NOT NULL

ORDER BY i.item_name;
";

            using var cmd = new SQLiteCommand(sql, con);
            cmd.Parameters.AddWithValue("@BuyerId", buyerId);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new SpecialPriceVM
                {
                    ItemId = rd.GetInt32(0),
                    ItemName = rd.GetString(1),
                    OriginalPrice = rd.GetDecimal(2),
                    SpecialPrice = rd.GetDecimal(3)
                });
            }

            return list;
        }

        // =========================================================
        // SAVE / UPDATE
        // =========================================================
        public static void SaveOrUpdate(int buyerId, int itemId, decimal price)
        {
            using var con = new SQLiteConnection(_conn);
            con.Open();

            string sql = @"
INSERT INTO special_prices (buyer_id, item_id, special_price)
VALUES (@BuyerId, @ItemId, @Price)
ON CONFLICT(buyer_id, item_id)
DO UPDATE SET special_price = excluded.special_price;
";

            using var cmd = new SQLiteCommand(sql, con);
            cmd.Parameters.AddWithValue("@BuyerId", buyerId);
            cmd.Parameters.AddWithValue("@ItemId", itemId);
            cmd.Parameters.AddWithValue("@Price", price);

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // DELETE
        // =========================================================
        public static void Delete(int buyerId, int itemId)
        {
            using var con = new SQLiteConnection(_conn);
            con.Open();

            string sql = @"
DELETE FROM special_prices
WHERE buyer_id = @BuyerId
  AND item_id = @ItemId;
";

            using var cmd = new SQLiteCommand(sql, con);
            cmd.Parameters.AddWithValue("@BuyerId", buyerId);
            cmd.Parameters.AddWithValue("@ItemId", itemId);

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // EFFECTIVE PRICE (used in Sales page)
        // =========================================================
        public static decimal GetEffectivePrice(int buyerId, int itemId)
        {
            using var con = new SQLiteConnection(_conn);
            con.Open();

            string sql = @"
SELECT COALESCE(
    (SELECT special_price FROM special_prices 
     WHERE buyer_id = @BuyerId AND item_id = @ItemId),
    (SELECT price FROM items WHERE item_id = @ItemId)
);
";

            using var cmd = new SQLiteCommand(sql, con);
            cmd.Parameters.AddWithValue("@BuyerId", buyerId);
            cmd.Parameters.AddWithValue("@ItemId", itemId);

            return Convert.ToDecimal(cmd.ExecuteScalar());
        }
    }
}
