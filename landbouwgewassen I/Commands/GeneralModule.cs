using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            await ReplyAsync($"👋 Hoi {Context.User.Mention}! Tot de volgende keer!");
        }

        [Command("coin")]
        public async Task CoinAsync()
        {
            int coins = Database.GetCoins(Context.User.Id);
            await ReplyAsync($"💰 {Context.User.Mention}, je hebt nu {coins} coins!");
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync("🌾 Beschikbare commando's:\n`!hallo`\n`!doei`\n`!coin`\n`!gewasinfo <naam>`\n`!farm`\n`!menu`");
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
                await ReplyAsync($"**{gewas.Naam}**\nGroeitijd (dagen): {gewas.groeitijd_dagen}\n{gewas.Beschrijving}");
            }
            else
            {
                await ReplyAsync($"Geen info gevonden voor '{gewasnaam}'. Probeer bv. tarwe, maïs, aardappel.");
            }
        }

        [Command("menu")]
        public async Task MenuAsync()
        {
            var builder = new ComponentBuilder()
                .WithButton("💰 Coin", "menu_coin", ButtonStyle.Primary)
                .WithButton("ℹ️ Help", "menu_help", ButtonStyle.Secondary)
                .WithButton("🌾 Gewasinfo", "menu_gewas", ButtonStyle.Success)
                .WithButton("🌱 Boerderij", "menu_farm", ButtonStyle.Secondary); // optioneel link naar farm

            var embed = new EmbedBuilder()
                .WithTitle("📋 Landbouwgewassen Menu")
                .WithDescription("Kies een optie hieronder:")
                .WithColor(Color.Green)
                .Build();

            var message = await ReplyAsync(embed: embed, components: builder.Build());

            var client = Context.Client as DiscordSocketClient;
            client.ButtonExecuted -= MenuButtonHandler;
            client.ButtonExecuted += MenuButtonHandler;
        }

        private async Task MenuButtonHandler(SocketMessageComponent component)
        {
            switch (component.Data.CustomId)
            {
                case "menu_coin":
                    Database.AddCoins(component.User.Id, 1);
                    int coins = Database.GetCoins(component.User.Id);
                    await component.RespondAsync($"💰 {component.User.Mention}, je hebt nu {coins} coins!", ephemeral: true);
                    break;

                case "menu_help":
                    await component.RespondAsync("🌾 Beschikbare commando's:\n`!hallo`\n`!doei`\n`!coin`\n`!gewasinfo <naam>`\n`!farm`", ephemeral: true);
                    break;

                case "menu_gewas":
                    await component.RespondAsync("Gebruik `!gewasinfo <naam>` (bijv. `!gewasinfo tarwe`).", ephemeral: true);
                    break;

                case "menu_farm":
                    await component.RespondAsync("Gebruik `!farm` om de boerderij te openen!", ephemeral: true);
                    break;

                default:
                    await component.RespondAsync("❓ Onbekende knop.", ephemeral: true);
                    break;
            }
        }

        private class Gewas
        {
            public string Naam { get; set; }
            public int groeitijd_dagen { get; set; }
            public string Beschrijving { get; set; }
        }
    }
}
