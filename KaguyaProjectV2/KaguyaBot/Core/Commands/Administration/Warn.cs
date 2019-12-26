﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.Core.Handlers;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using System;
using System.Threading.Tasks;
using KaguyaProjectV2.KaguyaBot.Core.Handlers.WarnEvent;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Administration
{
    public class Warn : ModuleBase<ShardedCommandContext>
    {
        private Server _server;
        private WarnedUser _warnedUser;
        private ICommandContext _context;

        public delegate void WarnHandler(object warn, WarnHandlerEventArgs args);
        public event WarnHandler Warning;

        protected void OnWarning(object warn, WarnHandlerEventArgs args)
        {
            Warning?.Invoke(warn, args);
        }

        [AdminCommand]
        [Command("Warn")]
        [Alias("w")]
        [Summary("Adds a warning to a user. If the server has a preconfigured warning-punishment scheme " +
                 "(via the `warnset` command), the user will be actioned accordingly. Upon receiving " +
                 "a warning, the user is DM'd with who warned them, why they were warned, and what server " +
                 "they received the warning from.")]
        [Remarks("<user> [reason]")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task AddWarn(IGuildUser user, [Remainder] string reason = null)
        {
            Server server = await ServerQueries.GetOrCreateServerAsync(Context.Guild.Id);

            if (reason == null)
                reason = "No reason specified.";

            var wu = new WarnedUser
            {
                ServerId = Context.Guild.Id,
                UserId = user.Id,
                ModeratorName = Context.User.ToString(),
                Reason = reason,
                Date = DateTime.Now.ToOADate()
            };

            await ServerQueries.AddWarnedUserAsync(wu);

            var warnEventArgs = new WarnHandlerEventArgs(server, wu, Context);
            OnWarning(this, warnEventArgs);

            await user.SendMessageAsync(embed: WarnEmbed(wu, Context).Build());
            await ReplyAsync(embed: Reply(wu, user).Build());

            if (server.IsPremium && server.ModLog != 0)
            {
                var premLog = new PremiumModerationLog
                {
                    Server = server,
                    Moderator = (SocketGuildUser) Context.User,
                    ActionRecipient = (SocketGuildUser) user,
                    Reason = reason,
                    Action = PremiumModActionHandler.WARN
                };

                await PremiumModerationLog.SendModerationLog(premLog);
            }

            await ServerQueries.UpdateServerAsync(server);
        }

        private static KaguyaEmbedBuilder WarnEmbed(WarnedUser user, ICommandContext context)
        {
            var embed = new KaguyaEmbedBuilder
            {
                Title = "⚠️ Warning Received",
                Description = $"Warned from `[Server: {context.Guild} | ID: {context.Guild.Id}]`\n" +
                              $"Warned by: `[User: {context.User} | ID: {context.User.Id}]`\n" +
                              $"Reason: `{user.Reason}`",
                Footer = new EmbedFooterBuilder
                {
                    Text =
                        $"You currently have {ServerQueries.GetWarningsForUserAsync(user.ServerId, user.UserId).Result.Count} warnings."
                }
            };
            embed.SetColor(EmbedColor.RED);
            return embed;
        }

        private static KaguyaEmbedBuilder Reply(WarnedUser user, IGuildUser warnedUser)
        {
            var warnCount = ServerQueries.GetWarningsForUserAsync(user.ServerId, user.UserId).Result.Count;
            var embed = new KaguyaEmbedBuilder
            {
                Description = $"Successfully warned user `{warnedUser}`\nReason: `{user.Reason}`",
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{warnedUser.Username} currently has {warnCount} warnings."
                }
            };
            return embed;
        }
    }

    public class WarnHandlerEventArgs : EventArgs
    {
        public WarnHandlerEventArgs(Server server, WarnedUser warnedUser, ICommandContext context)
        {
            this.server = server;
            this.warnedUser = warnedUser;
            this.context = context;
        }

        public readonly Server server;
        public readonly WarnedUser warnedUser;
        public readonly ICommandContext context;
    }
}