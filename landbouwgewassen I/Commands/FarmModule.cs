using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace LandbouwgewassenI.Commands
{
    public class FarmModule : ModuleBase<SocketCommandContext>
    {
        [Command("farm")]
        public async Task FarmAsync()
        {
            var builder = new ComponentBuilder();

            for (int row = 0; row < 4; row++)
            {
                var actionRow = new ActionRowBuilder(); // 1 rij
                for (int col = 0; col < 4; col++)
                {
                    string buttonId = $"farm_{row}_{col}";
                    var button = new ButtonBuilder()
                        .WithLabel("🌱")
                        .WithCustomId(buttonId)
                        .WithStyle(ButtonStyle.Primary);

                    actionRow.AddComponent(button);
                }

                builder.AddRow(actionRow); // voeg de hele rij toe
            }

            var embed = new EmbedBuilder()
                .WithTitle("🌾 Boerderij")
                .WithDescription("Klik op een vakje om te planten!")
                .WithColor(Color.Green)
                .Build();

            await ReplyAsync(embed: embed, components: builder.Build());
        }
    }
}
