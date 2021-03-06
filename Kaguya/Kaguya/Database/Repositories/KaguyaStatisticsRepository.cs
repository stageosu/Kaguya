﻿using Discord.WebSocket;
using Kaguya.Database.Context;
using Kaguya.Database.Interfaces;
using Kaguya.Database.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Database.Repositories
{
	public class KaguyaStatisticsRepository : RepositoryBase<KaguyaStatistics>, IKaguyaStatisticsRepository
	{
		private readonly DiscordShardedClient _client;
		private readonly CommandHistoryRepository _commandHistoryRepository;
		private readonly FishRepository _fishRepository;
		private readonly GambleHistoryRepository _gambleHistoryRepository;
		private readonly KaguyaServerRepository _kaguyaServerRepository;
		private readonly KaguyaUserRepository _kaguyaUserRepository;

		public KaguyaStatisticsRepository(KaguyaDbContext dbContext, KaguyaUserRepository kaguyaUserRepository,
			KaguyaServerRepository kaguyaServerRepository, DiscordShardedClient client, CommandHistoryRepository commandHistoryRepository,
			FishRepository fishRepository, GambleHistoryRepository gambleHistoryRepository) : base(dbContext)
		{
			_kaguyaUserRepository = kaguyaUserRepository;
			_kaguyaServerRepository = kaguyaServerRepository;
			_client = client;
			_commandHistoryRepository = commandHistoryRepository;
			_fishRepository = fishRepository;
			_gambleHistoryRepository = gambleHistoryRepository;
		}

		public async Task PostNewAsync()
		{
			var proc = Process.GetCurrentProcess();
			double ramUsage = (double) proc.PrivateMemorySize64 / 1000000; // Megabyte.

			int users = await _kaguyaUserRepository.GetCountAsync();
			int servers = await _kaguyaServerRepository.GetCountAsync();
			int curServers = _client.Guilds.Count;
			int shards = _client.Shards.Count;
			int commandsExecuted = await _commandHistoryRepository.GetSuccessfulCountAsync();
			int commandsLastTwentyFourHours = await _commandHistoryRepository.GetRecentSuccessfulCountAsync(TimeSpan.FromHours(24));

			int fish = await _fishRepository.GetCountAsync();
			int gambles = await _gambleHistoryRepository.GetCountAsync();
			int latency = _client.Latency;
			long coins = await _kaguyaUserRepository.CountCoinsAsync();

			var newStats = new KaguyaStatistics
			{
				Users = users,
				Servers = servers,
				ConnectedServers = curServers,
				Shards = shards,
				CommandsExecuted = commandsExecuted,
				CommandsExecutedTwentyFourHours = commandsLastTwentyFourHours,
				Fish = fish,
				Coins = coins,
				Gambles = gambles,
				RamUsageMegabytes = ramUsage,
				LatencyMilliseconds = latency,
				Version = Global.Version,
				Timestamp = DateTimeOffset.Now
			};

			await InsertAsync(newStats);
		}

		public async Task<KaguyaStatistics> GetMostRecentAsync()
		{
			return await Table.AsNoTracking().OrderByDescending(x => x.Id).FirstOrDefaultAsync();
		}
	}
}