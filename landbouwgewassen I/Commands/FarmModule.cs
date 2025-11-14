using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace LandbouwgewassenI.Commands
{
    public class FarmModule : ModuleBase<SocketCommandContext>
    {
        private static readonly string DataPath = Path.Combine(AppContext.BaseDirectory, "data", "gewassen.json");
        private Dictionary<string, Gewas> gewassen = new();

        public FarmModule()
        {
            if (File.Exists(DataPath))
            {
                var json = File.ReadAllText(DataPath);
                gewassen = JsonSerializer.Deserialize<Dictionary<string, Gewas>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new Dictionary<string, Gewas>();
            }
        }

        [Command("farm")]
        public async Task FarmAsync()
        {
            string currentCropKey = GetCurrentCrop(Context.User.Id);
            if (!gewassen.ContainsKey(currentCropKey))
            {
                await ReplyAsync($"❌ {currentCropKey} niet gevonden in gewassen.json.");
                return;
            }

            var crop = gewassen[currentCropKey];
            var builder = new ComponentBuilder();

            // 5x5 grid met het juiste icoon
            for (int row = 0; row < 5; row++)
            {
                var actionRow = new ActionRowBuilder();
                for (int col = 0; col < 5; col++)
                {
                    string customId = $"farm_{row}_{col}";
                    actionRow.WithButton(crop.Icon, customId, ButtonStyle.Primary);
                }
                builder.AddRow(actionRow);
            }

            var embed = new EmbedBuilder()
                .WithTitle($"{crop.Icon} {crop.Naam} Boerderij")
                .WithDescription("Klik op een vakje om het gewas te oogsten!")
                .WithColor(Color.Green)
                .Build();

            var message = await ReplyAsync(embed: embed, components: builder.Build());

            var client = Context.Client as DiscordSocketClient;
            client.ButtonExecuted -= FarmButtonHandler;
            client.ButtonExecuted += FarmButtonHandler;
        }

        private string GetCurrentCrop(ulong userId)
        {
            if (Database.GetLevel(userId, "tarwe") < 50)
                return "tarwe";
            if (Database.GetLevel(userId, "tarwe") >= 50 && Database.GetLevel(userId, "mais") < 50)
                return "mais";
            if (Database.GetLevel(userId, "mais") >= 50)
                return "aardappel";
            return "tarwe"; // fallback
        }

        private async Task FarmButtonHandler(SocketMessageComponent component)
        {
            if (!component.Data.CustomId.StartsWith("farm_")) return;

            string currentCropKey = GetCurrentCrop(component.User.Id);
            if (!gewassen.ContainsKey(currentCropKey)) return;

            var crop = gewassen[currentCropKey];

            // Haal het level van het gewas op
            int level = Database.GetLevel(component.User.Id, currentCropKey);
            int coinsToAdd = crop.coins * (level == 0 ? 1 : level);

            var oldComponent = component.Message.Components;
            var newBuilder = new ComponentBuilder();

            foreach (var rowComp in oldComponent)
            {
                if (rowComp is ActionRowComponent actionRow)
                {
                    var newRow = new ActionRowBuilder();
                    foreach (var btnComp in actionRow.Components)
                    {
                        if (btnComp is ButtonComponent btn)
                        {
                            if (btn.CustomId == component.Data.CustomId)
                                newRow.WithButton("⬛", btn.CustomId, ButtonStyle.Secondary, disabled: true);
                            else
                                newRow.WithButton(btn.Label, btn.CustomId, btn.Style, disabled: btn.IsDisabled);
                        }
                    }
                    newBuilder.AddRow(newRow);
                }
            }

            Database.AddCoins(component.User.Id, coinsToAdd);
            int totalCoins = Database.GetCoins(component.User.Id);

            await component.UpdateAsync(msg =>
            {
                msg.Components = newBuilder.Build();
                msg.Content = $"💰 {component.User.Mention} oogstte een {crop.Naam} (Lv {level}) en kreeg {coinsToAdd} coins! Totaal: {totalCoins} coins.";
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                var restoredBuilder = new ComponentBuilder();
                var oldRows = newBuilder.Build().Components;

                foreach (var rowComp in oldRows)
                {
                    if (rowComp is ActionRowComponent actionRow)
                    {
                        var newRow = new ActionRowBuilder();
                        foreach (var btnComp in actionRow.Components)
                        {
                            if (btnComp is ButtonComponent btn)
                            {
                                if (btn.CustomId == component.Data.CustomId)
                                    newRow.WithButton(crop.Icon, btn.CustomId, ButtonStyle.Primary);
                                else
                                    newRow.WithButton(btn.Label, btn.CustomId, btn.Style, disabled: btn.IsDisabled);
                            }
                        }
                        restoredBuilder.AddRow(newRow);
                    }
                }

                await component.Message.ModifyAsync(msg => msg.Components = restoredBuilder.Build());
            });
        }



        private class Gewas
        {
            public string Naam { get; set; }
            public int groeitijd_dagen { get; set; }
            public string Beschrijving { get; set; }
            public int coins { get; set; }
            public string Icon { get; set; }
        }
    }
}
