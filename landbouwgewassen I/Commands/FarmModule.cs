using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LandbouwgewassenI.Commands
{
    public class FarmModule : ModuleBase<SocketCommandContext>
    {
        private static readonly string DataPath = Path.Combine(AppContext.BaseDirectory, "data", "gewassen.json");

        private static readonly Dictionary<ulong, Dictionary<string, (DateTime startTime, string gewas)>> UserFarmData = new();
        private static readonly Dictionary<ulong, IUserMessage> UserFarmMessages = new();

        private static System.Timers.Timer refreshTimer;

        static FarmModule()
        {
            refreshTimer = new System.Timers.Timer(3000); // elke 3 sec
            refreshTimer.Elapsed += async (s, e) => await RefreshAllFarmMessages();
            refreshTimer.Start();
        }

        [Command("farm")]
        public async Task FarmCommandAsync()
        {
            if (!File.Exists(DataPath))
            {
                await ReplyAsync("❌ Gewassen data niet gevonden.");
                return;
            }

            var json = await File.ReadAllTextAsync(DataPath);
            var gewassen = JsonSerializer.Deserialize<Dictionary<string, Gewas>>(json) ?? new Dictionary<string, Gewas>();

            if (!gewassen.ContainsKey("tarwe"))
            {
                await ReplyAsync("❌ Gewas tarwe niet gevonden in de data.");
                return;
            }

            var tarwe = gewassen["tarwe"];

            if (!UserFarmData.ContainsKey(Context.User.Id))
                UserFarmData[Context.User.Id] = new Dictionary<string, (DateTime, string)>();

            var builder = BuildFarmButtons(Context.User.Id, tarwe);

            var embed = new EmbedBuilder()
                .WithTitle("🌾 Boerderij")
                .WithDescription("Klik op een vakje om te planten of voor coins als het gegroeid is!")
                .WithColor(Color.Green)
                .Build();

            var msg = await ReplyAsync(embed: embed, components: builder.Build());
            UserFarmMessages[Context.User.Id] = msg;
        }

        public static async Task HandleFarmButtonAsync(SocketMessageComponent component)
        {
            var userId = component.User.Id;

            if (!UserFarmData.ContainsKey(userId))
                UserFarmData[userId] = new Dictionary<string, (DateTime, string)>();

            var farmData = UserFarmData[userId];

            string key = component.Data.CustomId;

            var json = await File.ReadAllTextAsync(DataPath);
            var gewassen = JsonSerializer.Deserialize<Dictionary<string, Gewas>>(json) ?? new Dictionary<string, Gewas>();
            var tarwe = gewassen["tarwe"];

            if (!farmData.ContainsKey(key))
            {
                farmData[key] = (DateTime.UtcNow, "tarwe");
                await component.RespondAsync("🌱 Tarwe geplant! Kom later terug om te oogsten.", ephemeral: true);
            }
            else
            {
                var (startTime, _) = farmData[key];
                bool gegroeid = (DateTime.UtcNow - startTime).TotalSeconds >= tarwe.groeitijd_dagen;

                if (gegroeid)
                {
                    Database.AddCoins(userId, tarwe.coins);
                    farmData.Remove(key);
                    await component.RespondAsync($"💰 Je hebt {tarwe.coins} coins ontvangen!", ephemeral: true);
                }
                else
                {
                    await component.RespondAsync("❌ Dit gewas is nog niet gegroeid!", ephemeral: true);
                }
            }

            if (UserFarmMessages.ContainsKey(userId))
            {
                var msg = UserFarmMessages[userId];
                var builder = BuildFarmButtons(userId, tarwe);
                await msg.ModifyAsync(m => m.Components = builder.Build());
            }
        }

        private static ComponentBuilder BuildFarmButtons(ulong userId, Gewas tarwe)
        {
            var builder = new ComponentBuilder();
            var farmData = UserFarmData[userId];

            for (int row = 0; row < 4; row++)
            {
                var actionRow = new ActionRowBuilder();
                for (int col = 0; col < 4; col++)
                {
                    string key = $"cell_{row}_{col}";
                    bool gegroeid = false;

                    if (farmData.ContainsKey(key))
                    {
                        var (startTime, _) = farmData[key];
                        gegroeid = (DateTime.UtcNow - startTime).TotalSeconds >= tarwe.groeitijd_dagen;
                    }

                    actionRow.AddComponent(new ButtonBuilder()
                        .WithCustomId(key)
                        .WithLabel(gegroeid ? "🌾" : "❌")
                        .WithStyle(gegroeid ? ButtonStyle.Success : ButtonStyle.Secondary));
                }
                builder.AddRow(actionRow);
            }

            return builder;
        }

        private static async Task RefreshAllFarmMessages()
        {
            if (!File.Exists(DataPath)) return;

            var gewassen = JsonSerializer.Deserialize<Dictionary<string, Gewas>>(await File.ReadAllTextAsync(DataPath)) ?? new Dictionary<string, Gewas>();
            if (!gewassen.ContainsKey("tarwe")) return;
            var tarwe = gewassen["tarwe"];

            foreach (var kvp in UserFarmMessages)
            {
                ulong userId = kvp.Key;
                var msg = kvp.Value;

                if (!UserFarmData.ContainsKey(userId)) continue;

                var builder = BuildFarmButtons(userId, tarwe);
                await msg.ModifyAsync(m => m.Components = builder.Build());
            }
        }

        private class Gewas
        {
            public string naam { get; set; } = string.Empty;
            public int groeitijd_dagen { get; set; }
            public string beschrijving { get; set; } = string.Empty;
            public int coins { get; set; }
        }
    }

    public static class Database
    {
        private static readonly Dictionary<ulong, int> coins = new Dictionary<ulong, int>();

        public static void AddCoins(ulong userId, int amount)
        {
            if (!coins.ContainsKey(userId)) coins[userId] = 0;
            coins[userId] += amount;
        }

        public static int GetCoins(ulong userId)
        {
            if (!coins.ContainsKey(userId)) return 0;
            return coins[userId];
        }
    }
}
