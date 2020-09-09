﻿using Discord.WebSocket;
using Humanizer;
using KaguyaProjectV2.KaguyaBot.Core.Commands.Administration;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using KaguyaProjectV2.KaguyaBot.DataStorage.JsonStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogServices;

namespace KaguyaProjectV2.KaguyaBot.Core.Services
{
    public class AntiRaidService
    {
        public static async Task Initialize()
        {
            await Task.Run(() =>
            {
                var client = ConfigProperties.Client;
                client.UserJoined += async u =>
                {
                    var guild = u.Guild;
                    var server = await DatabaseQueries.GetOrCreateServerAsync(guild.Id);

                    if (server.AntiRaid == null || !server.AntiRaid.Any())
                        return;

                    var ar = server.AntiRaid.First();

                    if (server.AntiRaid.Count() > 1)
                    {
                        // Checks for duplicate antiraid entries in the database. There can only be one.
                        for (int i = 0; i < server.AntiRaid.Count() - 1; i++)
                        {
                            await DatabaseQueries.DeleteAsync(server.AntiRaid.ToList()[i]);
                        }

                        await ConsoleLogger.LogAsync($"Server {server.ServerId} had multiple antiraid configurations. " +
                                                     $"I have deleted all except one.", LogLvl.WARN);
                    }

                    if (!ServerTimers.CachedTimers.Any(x => x.ServerId == server.ServerId))
                    {
                        var newSt = new ServerTimer
                        {
                            ServerId = server.ServerId,
                            UserIds = new HashSet<ulong>
                            {
                                u.Id
                            }
                        };

                        ServerTimers.AddToCache(newSt);
                    }
                    else
                    {
                        var newIds = new HashSet<ulong>();
                        var existingIds = ServerTimers.CachedTimers.First(x => x.ServerId == server.ServerId).UserIds;

                        foreach (var id in existingIds)
                        {
                            newIds.Add(id);
                        }

                        newIds.Add(u.Id);
                        ServerTimers.ReplaceTimer(new ServerTimer
                        {
                            ServerId = server.ServerId,
                            UserIds = newIds
                        });
                    }

                    var timer = new Timer(ar.Seconds * 1000);
                    timer.Enabled = true;
                    timer.AutoReset = false;
                    timer.Elapsed += async (sender, args) =>
                    {
                        var existingObj = ServerTimers.CachedTimers.FirstOrDefault(x => x.ServerId == server.ServerId);

                        if (existingObj == null)
                            return;

                        if (existingObj.UserIds.Count >= ar.Users)
                        {
                            await ActionUsers(existingObj.UserIds, server.ServerId, ar.Action);
                        }

                        ServerTimers.CachedTimers.Remove(existingObj);
                    };
                };
            });
        }

        private static async Task ActionUsers(HashSet<ulong> userIds, ulong guildId, string action)
        {
            var guild = ConfigProperties.Client.GetGuild(guildId);
            var guildUsers = new List<SocketGuildUser>();

            foreach (var userId in userIds)
            {
                var guildUser = guild.GetUser(userId);
                
                if(guildUser != null)
                    guildUsers.Add(guildUser);
            }

            AntiRaidEvent.Trigger(guildUsers, guild, action.ApplyCase(LetterCasing.Sentence));

            if(guildUsers.Count == 0)
            {
                await ConsoleLogger.LogAsync($"The antiraid service was triggered in guild: {guild.Id} " + 
                "but no guild users were found from the provided set of IDs!", LogLvl.WARN);
                return;
            }

            switch (action.ToLower())
            {
                case "mute":
                    var mute = new Mute();
                    foreach (var user in guildUsers)
                    {
                        try
                        {
                            await mute.AutoMute(user);
                        }
                        catch (Exception)
                        {
                            await ConsoleLogger.LogAsync($"Attempted to auto-mute user " +
                                                         $"{user.ToString() ?? "NULL"} as " +
                                                         $"part of the antiraid service, but " +
                                                         $"an exception was thrown!!", LogLvl.ERROR);
                        }
                    }
                    break;
                case "kick":
                    var kick = new Kick();
                    foreach (var user in guildUsers)
                    {
                        try
                        {
                            await kick.AutoKickUserAsync(user, "Kaguya Anti-Raid protection.");
                        }
                        catch (Exception)
                        {
                            await ConsoleLogger.LogAsync($"Attempted to auto-kick user " +
                                                         $"{user.ToString() ?? "NULL"} as " +
                                                         $"part of the antiraid service, but " +
                                                         $"an exception was thrown!!", LogLvl.ERROR);
                        }
                    }
                    break;
                case "shadowban":
                    var sb = new Shadowban();
                    foreach (var user in guildUsers)
                    {
                        try
                        {
                            await sb.AutoShadowbanUserAsync(user);

                        }
                        catch (Exception)
                        {
                            await ConsoleLogger.LogAsync($"Attempted to auto-shadowban user " +
                                                         $"{user.ToString() ?? "NULL"} as " +
                                                         $"part of the antiraid service, but " +
                                                         $"an exception was thrown!!", LogLvl.ERROR);
                        }
                    }
                    break;
                case "ban":
                    var ban = new Ban();
                    foreach (var user in guildUsers)
                    {
                        try
                        {
                            await ban.AutoBanUserAsync(user, "Kaguya Anti-Raid protection.");
                        }
                        catch (Exception)
                        {
                            await ConsoleLogger.LogAsync($"Attempted to auto-ban user " +
                                                         $"{user.ToString() ?? "NULL"} as " +
                                                         $"part of the antiraid service, but " +
                                                         $"an exception was thrown!!", LogLvl.ERROR);
                        }
                    }
                    break;
                default:
                    await ConsoleLogger.LogAsync("Antiraid service triggered, but no users actioned. " +
                                                 "Antiraid action string is different than expected. " +
                                                 "Expected \"mute\" \"kick\" \"shadowban\" OR \"ban\". " +
                                                 $"Received: '{action.ToLower()}'. " +
                                                 $"Guild: {guildId}.", LogLvl.ERROR);
                    break;
            }

            await ConsoleLogger.LogAsync($"Antiraid: Successfully actioned {guildUsers.Count:N0} users in guild {guild.Id}.", LogLvl.INFO);
        }
    }

    public class ServerTimer
    {
        public ulong ServerId { get; set; }
        public HashSet<ulong> UserIds { get; set; }
    }

    public static class ServerTimers
    {
        public static List<ServerTimer> CachedTimers { get; set; } = new List<ServerTimer>();

        /// <summary>
        /// Adds a timer to the cache.
        /// </summary>
        /// <param name="stObj"></param>
        public static void AddToCache(ServerTimer stObj)
        {
            CachedTimers.Add(stObj);
        }

        public static void ReplaceTimer(ServerTimer stObj)
        {
            var existingObj = CachedTimers.FirstOrDefault(x => x.ServerId == stObj.ServerId);

            CachedTimers.Remove(existingObj);
            CachedTimers.Add(stObj);
        }
    }

    public static class AntiRaidEvent
    {
        public static event Func<AntiRaidEventArgs, Task> OnRaid;

        public static void Trigger(List<SocketGuildUser> users, SocketGuild guild, string punishment)
        {
            AntiRaidEventTrigger(new AntiRaidEventArgs(users, guild, punishment));
        }

        private static void AntiRaidEventTrigger(AntiRaidEventArgs e)
        {
            OnRaid?.Invoke(e);
        }
    }

    public class AntiRaidEventArgs : EventArgs
    {
        public List<SocketGuildUser> GuildUsers { get; }
        public SocketGuild SocketGuild { get; }
        public string Punishment { get; }

        public AntiRaidEventArgs(List<SocketGuildUser> users, SocketGuild guild, string punishment)
        {
            this.GuildUsers = users;
            this.SocketGuild = guild;
            this.Punishment = punishment;
        }
    }
}
