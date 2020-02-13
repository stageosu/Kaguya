﻿using Discord.Commands;
using Discord.WebSocket;
using KaguyaProjectV2.KaguyaBot.Core.Extensions;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using KaguyaProjectV2.KaguyaBot.Core.Osu;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogService;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using KaguyaProjectV2.KaguyaBot.DataStorage.JsonStorage;
using OsuSharp;
using OsuSharp.Oppai;
using System.Linq;
using System.Threading.Tasks;

namespace KaguyaProjectV2.KaguyaBot.Core.Services
{
    public class AutomaticBeatmapLinkParserService : OsuBase
    {
        public static async Task LinkParserMethod(SocketMessage s, ShardedCommandContext context)
        {
            var user = await DatabaseQueries.GetOrCreateUserAsync(context.User.Id);
            var server = await DatabaseQueries.GetOrCreateServerAsync(context.Guild.Id);

            var embed = new KaguyaEmbedBuilder();

            user.ActiveRateLimit++;
            if (user.IsBlacklisted)
                return;

            string link = $"{s}";
            string mapID = link.Split('/').Last(); //Gets the map's ID from the link.
            if (mapID.Contains('?'))
                mapID = mapID.Replace("?m=0", "");

            var mapData = await client.GetBeatmapByIdAsync((long) mapID.AsUlong());

            string status = "";
            var state = mapData.State;

            var approvedDate = mapData.ApprovedDate;
            status = state switch
            {
                // ReSharper disable PossibleInvalidOperationException
                BeatmapState.Graveyard | BeatmapState.WorkInProgress | BeatmapState.Pending =>
                $"Last updated on {mapData.LastUpdate.Value.LocalDateTime.ToShortDateString()}",
                BeatmapState.Ranked => $"Ranked on {approvedDate.Value.LocalDateTime.ToShortDateString()}",
                BeatmapState.Approved => $"Approved on {approvedDate.Value.LocalDateTime.ToShortDateString()}",
                BeatmapState.Qualified => $"Qualified on {approvedDate.Value.LocalDateTime.ToShortDateString()}",
                BeatmapState.Loved => $"Loved 💙 on {approvedDate.Value.LocalDateTime.ToShortDateString()}",
                // ReSharper restore PossibleInvalidOperationException
                _ => status
            };

            string lengthValue = mapData.TotalLength.ToString(@"mm\:ss");
            var pp95 = await mapData.GetPPAsync(95f);
            var pp98 = await mapData.GetPPAsync(98f);
            var pp99 = await mapData.GetPPAsync(99f);
            var pp100 = await mapData.GetPPAsync(100f);

            embed.WithAuthor(author =>
            {
                author.Name = $"{mapData.Title} by {mapData.Author}";
                author.Url = $"https://osu.ppy.sh/b/{mapID}";
                author.IconUrl = $"https://a.ppy.sh/{mapData.AuthorId}";
            });

            embed.WithDescription(
                $"**{mapData.Title} [{mapData.Difficulty}]** by **{mapData.Artist}**" +
                $"\n" +
                $"\n<:total_length:630131957598126120> **Total Length:** {lengthValue} <:bpm:630131958046785563> **BPM:** {mapData.Bpm:N1}" +
                $"\n**Star Rating:** `{mapData.StarRating:N2} ☆` **Maximum Combo:** `{mapData.MaxCombo:N0}x`" +
                $"\n**Download:** [[Beatmap]]({mapData.BeatmapUri}/download)" +
                $"[(without video)](https://osu.ppy.sh/d/{mapData.BeatmapUri}n)" +
                $"\n" +
                $"\n**CS:** `{mapData.CircleSize:N2} ` **AR:** `{mapData.ApproachRate:N2}` " +
                $"**OD:** `{mapData.OverallDifficulty:N2}` **HP:** `{mapData.HpDrain:N2}`" +
                $"\n" +
                $"\n**95% FC:** `{pp95.Pp:N0}pp` **98% FC:** `{pp98.Pp:N0}pp`" +
                $"\n**99% FC:** `{pp99.Pp:N0}pp` **100% FC (SS):** `{pp100.Pp:N0}pp`");
            embed.WithFooter($"Status: {status} | 💙 Amount: {mapData.FavoriteCount:N0}");

            await context.Channel.SendEmbedAsync(embed);
            await ConsoleLogger.LogAsync(
                $"osu! beatmap automatically parsed in guild {server.ServerId} by user {user.UserId}",
                LogLvl.DEBUG);

            user.OsuBeatmapsLinked++;
            await DatabaseQueries.UpdateAsync(user);
        }
    }
}