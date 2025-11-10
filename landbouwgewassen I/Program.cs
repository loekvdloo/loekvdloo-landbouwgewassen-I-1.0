using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace LandbouwgewassenI
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;

        static async Task Main(string[] args)
            => await new Program().MainAsync();

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

            // Registreer commands, zonder DI
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            _client.MessageReceived += HandleCommandAsync;



            if (string.IsNullOrWhiteSpace(bott))
            {
                Console.WriteLine("Fout:  is niet ingesteld.");
                return;
            }

            await _client.LoginAsync(TokenType.Bot, bott);
            await _client.StartAsync();

            Console.WriteLine("🌱 Landbouwgewassen I is gestart!");
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage rawMsg)
        {
            if (!(rawMsg is SocketUserMessage msg)) return;
            if (msg.Author.IsBot) return;

            int argPos = 0;
            if (!(msg.HasCharPrefix('!', ref argPos)
                  || msg.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
            
            var context = new SocketCommandContext(_client, msg);
            var result = await _commands.ExecuteAsync(context, argPos, null);

            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync($"Er ging iets mis: {result.ErrorReason}");
            }
        }
    }
}
