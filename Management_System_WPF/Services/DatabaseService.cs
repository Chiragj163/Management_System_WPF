using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace Management_System.Services
{
    internal class DatabaseService
    {
        public static SQLiteConnection GetConnection()
        {
            string dbPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Database",
                "factory.db");

            return new SQLiteConnection($"Data Source={dbPath};Version=3;");
        }
    }
}
