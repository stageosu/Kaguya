using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Interactivity;
using Kaguya.Database.Model;
using Kaguya.Internal.Events.ArgModels;
using Kaguya.Internal.Music;
using Kaguya.Internal.Services;
using Kaguya.Internal.Services.Models;
using Kaguya.Internal.Services.Recurring;
using Microsoft.Extensions.Logging;
using Victoria;

namespace Kaguya.Internal.Events
{
    public class Events
    {
        private readonly DiscordShardedClient _client;
        private readonly IAntiraidService _antiraidService;
        private readonly LavaNode _lavaNode;
        private readonly AudioService _audioService;
        private readonly GuildLoggerService _guildLoggerService;
        private readonly ILogger<Events> _logger;

        /// <summary>
        /// Fires whenever an Antiraid is detected in a guild and a user has been actioned.
        /// </summary>
        public static event Action<AdminAction, SocketUser> OnAntiraid;
        /// <summary>
        /// Invokes the <see cref="OnAntiraid"/> event.
        /// </summary>
        /// <param name="adminAction"></param>
        /// <param name="socketUser"></param>
        public static void OnAntiraidTrigger(AdminAction adminAction, SocketUser socketUser) => OnAntiraid?.Invoke(adminAction, socketUser);
        
        /// <summary>
        /// Fired when a filtered word is detected in a guild.
        /// </summary>
        public static event Action<FilteredWordEventData> OnFilteredWordDetected;
        /// <summary>
        /// Invokes the <see cref="OnFilteredWordDetected"/> event.
        /// </summary>
        /// <param name="data"></param>
        public static void OnFilteredWordDetectedTrigger(FilteredWordEventData data) => OnFilteredWordDetected?.Invoke(data);

        public Events(ILogger<Events> logger, DiscordShardedClient client, IAntiraidService antiraidService,
            LavaNode lavaNode, AudioService audioService, GuildLoggerService guildLoggerService)
        {
            _logger = logger;
            _client = client;
            _antiraidService = antiraidService;
            _lavaNode = lavaNode;
            _audioService = audioService;
            _guildLoggerService = guildLoggerService;
        }

        public void InitEvents()
        {
            var eventImplementations = new EventImplementations(_antiraidService);
            
            _logger.LogDebug("Kaguya Events initialized.");
            
            _client.ShardReady += ClientOnShardReady;
            _client.UserJoined += eventImplementations.OnUserJoined;

            _client.MessageDeleted += _guildLoggerService.LogMessageDeletedAsync;
            _client.MessageUpdated += _guildLoggerService.LogMessageUpdatedAsync;
            _client.UserJoined += _guildLoggerService.LogUserJoinedAsync;
            _client.UserLeft += _guildLoggerService.LogUserLeftAsync;
            _client.UserBanned += _guildLoggerService.LogUserBannedAsync;
            _client.UserUnbanned += _guildLoggerService.LogUserUnbannedAsync;
            _client.UserVoiceStateUpdated += _guildLoggerService.LogUserVoiceStateUpdatedAsync;
            
            OnAntiraid += async (a, u) => await _guildLoggerService.LogAntiRaidAsync(a, u);
            OnFilteredWordDetected += async d => await _guildLoggerService.LogFilteredWordAsync(d);
            
            _lavaNode.OnTrackStarted += _audioService.OnTrackStarted;
            _lavaNode.OnTrackEnded += _audioService.OnTrackEnded;
        }

        private async Task ClientOnShardReady(DiscordSocketClient arg)
        {
            Global.AddReadyShard(arg.ShardId);

            if (!_lavaNode.IsConnected)
            {
                await _lavaNode.ConnectAsync();
                _logger.LogInformation($"Lava Node connected (shard {arg.ShardId:N0})");
            }
        }
    }
}