using Discord.Commands;
using System.IO;

namespace LandbouwgewassenI.Commands
{
    public class ResetModule : ModuleBase<SocketCommandContext>
    {
        private readonly ulong BotOwnerId = 818112857782485035;

        [Command("resetdb")]
        public async Task ResetDbAsync()
        {
            if (Context.User.Id != BotOwnerId)
            {
                await ReplyAsync("❌ Jij mag dit niet gebruiken!");
                return;
            }

            string path = Path.Combine(AppContext.BaseDirectory, "data", "botdata.db");

            if (File.Exists(path))
            {
                try
                {
                    // Alle verbindingen in de Database-class sluiten
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    File.Delete(path);
                    await ReplyAsync("✅ Database is verwijderd! De bot maakt een nieuwe aan bij de volgende start.");
                }
                catch (IOException ex)
                {
                    await ReplyAsync($"❌ Kan database niet verwijderen: {ex.Message}");
                }
            }
            else
            {
                await ReplyAsync("ℹ️ Database bestaat nog niet, er is niets te verwijderen.");
            }
        }

    }
}
