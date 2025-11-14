using Microsoft.Data.Sqlite;
using System;
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
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS users (
                user_id TEXT PRIMARY KEY,
                coins INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS gewas_levels (
                user_id TEXT NOT NULL,
                gewas TEXT NOT NULL,
                level INTEGER NOT NULL,
                PRIMARY KEY(user_id, gewas)
            );
            ";
            command.ExecuteNonQuery();
        }

        // ---------------- Coins ----------------
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

        // ---------------- Levels / Upgrade ----------------
        public static int GetLevel(ulong userId, string gewas)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT level FROM gewas_levels WHERE user_id = $id AND gewas = $gewas";
            command.Parameters.AddWithValue("$id", userId.ToString());
            command.Parameters.AddWithValue("$gewas", gewas);

            var result = command.ExecuteScalar();
            return result == null ? 0 : Convert.ToInt32(result);
        }

        public static bool UpgradeGewas(ulong userId, string gewas, int cost)
        {
            int coins = GetCoins(userId);
            if (coins < cost)
                return false;

            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            var transaction = connection.BeginTransaction();

            // Coins aftrekken
            var coinCmd = connection.CreateCommand();
            coinCmd.CommandText = "UPDATE users SET coins = coins - $cost WHERE user_id = $id";
            coinCmd.Parameters.AddWithValue("$cost", cost);
            coinCmd.Parameters.AddWithValue("$id", userId.ToString());
            coinCmd.ExecuteNonQuery();

            // Level verhogen
            var levelCmd = connection.CreateCommand();
            levelCmd.CommandText = @"
                INSERT INTO gewas_levels (user_id, gewas, level)
                VALUES ($id, $gewas, 1)
                ON CONFLICT(user_id, gewas)
                DO UPDATE SET level = level + 1;
            ";
            levelCmd.Parameters.AddWithValue("$id", userId.ToString());
            levelCmd.Parameters.AddWithValue("$gewas", gewas);
            levelCmd.ExecuteNonQuery();

            transaction.Commit();
            return true;
        }
    }
}
