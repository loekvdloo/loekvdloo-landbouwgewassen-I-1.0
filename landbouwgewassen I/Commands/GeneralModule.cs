using Discord.Commands;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Discord;

namespace LandbouwgewassenI.Commands
{
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        private static readonly string DataPath = Path.Combine(AppContext.BaseDirectory, "data", "gewassen.json");

        [Command("hallo")]
        public async Task HalloAsync()
        {
            await ReplyAsync($"👋 Hoi {Context.User.Mention}! Ik ben Landbouwgewassen I — jouw boerderij-assistent!");
        }

        [Command("doei")]
        public async Task DoeiAsync()
        {
            await ReplyAsync($"👋 Hoi {Context.User.Mention}! Ik ben Landbouwgewassen I — jouw boerderij-assistent!");
        }

        [Command("coin")]
        public async Task CoinAsync()
        {
            var userId = Context.User.Id;
            Database.AddCoins(userId, 1);

            int coins = Database.GetCoins(userId);
            await ReplyAsync($"💰 {Context.User.Mention}, je hebt nu {coins} coins!");
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync("🌾 Beschikbare commando's:\n`!hallo`\n`!gewasinfo <naam>` (bv. `!gewasinfo tarwe`)");
        }

        [Command("gewasinfo")]
        public async Task GewasInfoAsync([Remainder] string gewasnaam = null)
        {
            if (string.IsNullOrWhiteSpace(gewasnaam))
            {
                await ReplyAsync("Geef een gewasnaam op, bv. `!gewasinfo tarwe`.");
                return;
            }

            if (!File.Exists(DataPath))
            {
                await ReplyAsync("Data bestand niet gevonden. Zorg dat `data/gewassen.json` bestaat.");
                return;
            }

            var json = await File.ReadAllTextAsync(DataPath);
            var doc = JsonSerializer.Deserialize<Dictionary<string, Gewas>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (doc != null && doc.TryGetValue(gewasnaam.ToLowerInvariant(), out var gewas))
            {
                var reply = $"**{gewas.Naam}**\nGroeitijd (dagen): {gewas.groeitijd_dagen}\n{gewas.Beschrijving}";
                await ReplyAsync(reply);
            }
            else
            {
                await ReplyAsync($"Geen info gevonden voor '{gewasnaam}'. Probeer bv. tarwe, maïs, aardappel.");
            }

        }

        [Command("menu")]
        private async Task MenuAsync()
        {
            var builder = new ComponentBuilder()
                .WithButton("💰 Coin", "menu_coin", ButtonStyle.Primary)
                .WithButton("ℹ️ Help", "menu_help", ButtonStyle.Secondary)
                .WithButton("🌾 Gewasinfo", "menu_gewas", ButtonStyle.Success);

            var embed = new EmbedBuilder()
                .WithTitle("📋 Landbouwgewassen Menu")
                .WithDescription("Kies een optie hieronder:")
                .WithColor(Color.Green)
                .Build();

            await ReplyAsync(embed: embed, components: builder.Build());
        }

        private class Gewas
        {
            public string Naam { get; set; }
            public int groeitijd_dagen { get; set; }
            public string Beschrijving { get; set; }
        }
    }
}
