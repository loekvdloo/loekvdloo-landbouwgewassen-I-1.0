using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LandbouwgewassenI.Commands
{
    public class UpgradeModule : ModuleBase<SocketCommandContext>
    {
        private Dictionary<string, Gewas> gewassen;

        public UpgradeModule()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "data", "gewassen.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("Gewassen.json niet gevonden!", path);

            var json = File.ReadAllText(path);
            gewassen = JsonSerializer.Deserialize<Dictionary<string, Gewas>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        [Command("upgrade")]
        public async Task UpgradeMenuAsync()
        {
            var builder = BuildUpgradeButtons(Context.User.Id);

            var embed = new EmbedBuilder()
                .WithTitle("🌾 Gewas Upgrade Menu")
                .WithDescription("Klik op een gewas om het te upgraden!")
                .WithColor(Color.Green)
                .Build();

            await ReplyAsync(embed: embed, components: builder.Build());

            var client = Context.Client as DiscordSocketClient;
            client.ButtonExecuted -= UpgradeButtonHandler;
            client.ButtonExecuted += UpgradeButtonHandler;
        }

        private async Task UpgradeButtonHandler(SocketMessageComponent component)
        {
            if (!component.Data.CustomId.StartsWith("upgrade_")) return;

            await component.DeferAsync();

            string gewasKey = component.Data.CustomId.Replace("upgrade_", "").ToLower();
            if (!gewassen.ContainsKey(gewasKey)) return;

            int playerLevel = Database.GetLevel(component.User.Id, gewasKey);
            int cost = 10 * (playerLevel == 0 ? 1 : playerLevel); // Level 0 is betaalbaar

            bool success = Database.UpgradeGewas(component.User.Id, gewasKey, cost);

            if (success)
            {
                int newLevel = Database.GetLevel(component.User.Id, gewasKey);
                await component.FollowupAsync($"✅ {component.User.Mention}, je hebt {gewassen[gewasKey].naam} geüpgraded naar level {newLevel}! Kosten: {cost} coins.", ephemeral: true);
            }
            else
            {
                await component.FollowupAsync($"❌ {component.User.Mention}, niet genoeg coins voor {gewassen[gewasKey].naam} (kost {cost}).", ephemeral: true);
            }

            var builder = BuildUpgradeButtons(component.User.Id);
            await component.ModifyOriginalResponseAsync(msg => msg.Components = builder.Build());
        }

        private ComponentBuilder BuildUpgradeButtons(ulong userId)
        {
            var builder = new ComponentBuilder();

            foreach (var g in gewassen.Values)
            {
                string key = g.naam.ToLower();
                int lv = Database.GetLevel(userId, key);

                // Upgradekosten berekenen
                int cost = 10 * (lv == 0 ? 1 : lv);

                // Unlock logic
                bool unlocked = key switch
                {
                    "tarwe" => true, // altijd beschikbaar
                    "mais" => Database.GetLevel(userId, "tarwe") >= 50,
                    "aardappel" => Database.GetLevel(userId, "mais") >= 50,
                    _ => false
                };

                // Knop label inclusief level en kost
                builder.WithButton($"{g.icon} {g.naam} (Lv {lv}) - {cost} coins", $"upgrade_{key}", ButtonStyle.Primary, disabled: !unlocked);
            }

            return builder;
        }


        private class Gewas
        {
            public string naam { get; set; }
            public int coins { get; set; }
            public string icon { get; set; }
        }
    }
}
