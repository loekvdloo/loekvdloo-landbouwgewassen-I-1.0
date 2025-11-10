using Discord.Commands;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace LandbouwgewassenI.Commands
{
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        private const string DataPath = "data/gewassen.json";

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
                var reply = $"**{gewas.Naam}**\nGroeitijd (dagen): {gewas.GroeitijdDays}\n{gewas.Beschrijving}";
                await ReplyAsync(reply);
            }
            else
            {
                await ReplyAsync($"Geen info gevonden voor '{gewasnaam}'. Probeer bv. tarwe, maïs, aardappel.");
            }
        }

        private class Gewas
        {
            public string Naam { get; set; }
            public int GroeitijdDays { get; set; }
            public string Beschrijving { get; set; }
        }
    }
}
