using Discord;
using Discord.Commands;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using System.Threading.Tasks;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using KaguyaProjectV2.KaguyaBot.Core.Extensions;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.EXP
{
    public class GlobalFishLeaderboard : KaguyaBase
    {
        [ExpCommand]
        [Command("FishLeaderboard")]
        [Alias("flb")]
        [Summary("Allows you to see the leaderboard of Kaguya's top fishermen!")]
        [Remarks("")]
        public async Task Command()
        {
            var players = await DatabaseQueries.GetLimitAsync<User>(10, x => x.FishExp > 0, x => x.FishExp, true);

            var embed = new KaguyaEmbedBuilder();
            embed.Title = "Kaguya Fishing Leaderboard";

            foreach(var player in players)
            {
                var guildUser = Context.Guild.GetUser(player.UserId);

                embed.Fields.Add(new EmbedFieldBuilder
                {
                    Name = ($"[{guildUser?.ToString() ?? $"Unknown User: {player.UserId}"}]"),
                    Value = $"Fish Level: {player.FishLevel():0} | Fish Exp: {player.FishExp:N0}"
                });
            }

            await SendEmbedAsync(embed);
        }
    }
}