﻿using Discord;
using Discord.Commands;
using KaguyaProjectV2.Core.Handlers;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using System;

namespace KaguyaProjectV2.KaguyaBot.Core.Handlers
{
    public static class ExperienceHandler
    {
        public static async void AddEXP(User user, ICommandContext context)
        {
            Server server = ServerQueries.GetServer(context.Guild.Id);

            // If the user can receive exp, give them between 5 and 8.
            if (CanGetExperience(user))
            {
                var levelAnnouncementChannel = await context.Guild.GetChannelAsync(server.LogLevelAnnouncements);
                double oldLevel = ReturnLevel(user);

                Random r = new Random();
                int exp = r.Next(5, 8);

                user.Experience += exp;
                user.LatestEXP = DateTime.Now.ToOADate();
                Users.UpdateUser(user);

                double newLevel = ReturnLevel(user);
                await Logger.Logger.Log($"User has received {exp} exp. [ID: {user.Id} | New EXP: {user.Experience.ToString("N0")}]", DataStorage.JsonStorage.LogLevel.TRACE);

                if(HasLeveledUp(oldLevel, newLevel))
                {
                    await Logger.Logger.Log($"User has leveled up. [ID: {user.Id} | Level: {newLevel} | Experience: {user.Experience}]", DataStorage.JsonStorage.LogLevel.INFO);
                    if (levelAnnouncementChannel != null)
                    {
                        await (levelAnnouncementChannel as IMessageChannel).SendMessageAsync(embed: LevelUpEmbed(user, context));
                        return;
                    }

                    await context.Channel.SendMessageAsync(embed: LevelUpEmbed(user, context));
                }
            }
        }

        private static bool CanGetExperience(User user)
        {
            if (DateTime.Now.AddSeconds(0).ToOADate() >= user.LatestEXP) //2 minutes in OA date time.
            {
                return true;
            }
            return false;
        }

        private static double ReturnLevel(User user)
        {
            return Math.Sqrt(user.Experience / 8 + -8);
        }

        private static bool HasLeveledUp(double oldLevel, double newLevel)
        {
            if (Math.Floor(oldLevel) < Math.Floor(newLevel))
            {
                return true;
            }
            return false;
        }

        public static Embed LevelUpEmbed(User user, ICommandContext context)
        {
            KaguyaEmbedBuilder embed = new KaguyaEmbedBuilder
            {
                Title = "Level Up!",
                Description = $"{context.User.Username} just leveled up! \n[Level: {ReturnLevel(user)} | Experience Points: {user.Experience.ToString("N0")}]\nRank: [COMING SOON]",
                ThumbnailUrl = Global.GlobalProperties.Client.GetUser(user.Id).GetAvatarUrl()
            };

            return embed.Build();
        }
    }
}
