using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LandbouwgewassenI.Commands;

namespace LandbouwgewassenI
{
    class Program
    {
        private DiscordSocketClient? _client;
        private CommandService? _commands;


        static async Task Main(string[] args)
        {
            await new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                               | GatewayIntents.GuildMessages
                               | GatewayIntents.MessageContent
            });

            _commands = new CommandService();

            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            // Voeg modules toe
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            _client.MessageReceived += HandleCommandAsync;

            // ButtonExecuted handler
            _client.ButtonExecuted += async component =>
            {
                if (component.Data.CustomId.StartsWith("cell_"))
                {
                    await FarmModule.HandleFarmButtonAsync(component);
                }
                else
                {
                    await HandleMenuButtons(component);
                }
            };

            if (string.IsNullOrWhiteSpace(bott))
            {
                Console.WriteLine("❌ Bot token is niet ingesteld.");
                return;
            }

            await _client.LoginAsync(TokenType.Bot, bott);
            await _client.StartAsync();

            Console.WriteLine("🌱 Bot gestart!");
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // Command handler
        private async Task HandleCommandAsync(SocketMessage rawMsg)
        {
            if (!(rawMsg is SocketUserMessage msg)) return;
            if (msg.Author.IsBot) return;

            int argPos = 0;
            if (!(msg.HasCharPrefix('!', ref argPos)
                  || msg.HasMentionPrefix(_client.CurrentUser!, ref argPos))) return;

            var context = new SocketCommandContext(_client!, msg);
            var result = await _commands!.ExecuteAsync(context, argPos, null);

            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync($"⚠️ Er ging iets mis: {result.ErrorReason}");
            }
        }

        // Menu-knoppen handler
        private async Task HandleMenuButtons(SocketMessageComponent component)
        {
            switch (component.Data.CustomId)
            {
                case "menu_coin":
                    Database.AddCoins(component.User.Id, 1);
                    int coins = Database.GetCoins(component.User.Id);
                    await component.RespondAsync($"💰 {component.User.Mention}, je hebt nu {coins} coins!");
                    break;

                case "menu_help":
                    await component.RespondAsync("🌾 Beschikbare commando's:\n`!hallo`\n`!gewasinfo <naam>`\n`!coin`\n`!doei`");
                    break;

                case "menu_gewas":
                    await component.RespondAsync("Gebruik `!gewasinfo <naam>` (bijv. `!gewasinfo tarwe`).");
                    break;

                default:
                    await component.RespondAsync("❓ Onbekende knop.", ephemeral: true);
                    break;
            }
        }
    }
}
