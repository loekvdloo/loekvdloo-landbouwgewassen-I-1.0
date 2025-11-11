using Microsoft.Data.Sqlite;
using System.IO;

namespace LandbouwgewassenI
{
    public static class Database
    {
        private static readonly string DbPath = Path.Combine("data", "botdata.db");

        static Database()
        {
            if (!Directory.Exists("data"))
                Directory.CreateDirectory("data");

            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS users (
                user_id TEXT PRIMARY KEY,
                coins INTEGER NOT NULL
            );
            ";
            command.ExecuteNonQuery();
        }

        public static int GetCoins(ulong userId)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT coins FROM users WHERE user_id = $id";
            command.Parameters.AddWithValue("$id", userId.ToString());

            var result = command.ExecuteScalar();
            return result == null ? 0 : Convert.ToInt32(result);
        }

        public static void AddCoins(ulong userId, int amount)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO users (user_id, coins)
                VALUES ($id, $coins)
                ON CONFLICT(user_id)
                DO UPDATE SET coins = coins + $coins;
            ";
            command.Parameters.AddWithValue("$id", userId.ToString());
            command.Parameters.AddWithValue("$coins", amount);
            command.ExecuteNonQuery();
        }
    }
}
