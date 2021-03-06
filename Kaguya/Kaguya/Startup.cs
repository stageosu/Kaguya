using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Interactivity;
using Kaguya.Database.Context;
using Kaguya.Database.Repositories;
using Kaguya.Discord;
using Kaguya.Discord.Options;
using Kaguya.External.Services.TopGg;
using Kaguya.Internal;
using Kaguya.Internal.Events;
using Kaguya.Internal.Memory;
using Kaguya.Internal.Music;
using Kaguya.Internal.Services;
using Kaguya.Internal.Services.Recurring;
using Kaguya.Options;
using Kaguya.Web.Options;
using Kaguya.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NekosSharp;
using OsuSharp;
using System;
using Victoria;

namespace Kaguya
{
	public class Startup
	{
		public Startup(IConfiguration configuration) { this.Configuration = configuration; }
		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<AdminConfigurations>(this.Configuration.GetSection(AdminConfigurations.Position));
			services.Configure<DiscordConfigurations>(this.Configuration.GetSection(DiscordConfigurations.Position));
			services.Configure<TopGgConfigurations>(this.Configuration.GetSection(TopGgConfigurations.Position));

			services.AddDbContextPool<KaguyaDbContext>(builder =>
			{
				builder.UseMySql(this.Configuration.GetConnectionString("Database"),
					ServerVersion.AutoDetect(this.Configuration.GetConnectionString("Database")));
			});

			// All database repositories are added as scoped here.
			services.AddScoped<AdminActionRepository>();
			services.AddScoped<AntiraidConfigRepository>();
			services.AddScoped<AutoRoleRepository>();
			services.AddScoped<BlacklistedEntityRepository>();
			services.AddScoped<CommandHistoryRepository>();
			services.AddScoped<EightballRepository>();
			services.AddScoped<FavoriteTrackRepository>();
			services.AddScoped<FilteredWordRepository>();
			services.AddScoped<FishRepository>();
			services.AddScoped<GambleHistoryRepository>();
			services.AddScoped<KaguyaServerRepository>();
			services.AddScoped<KaguyaStatisticsRepository>();
			services.AddScoped<KaguyaUserRepository>();
			services.AddScoped<LogConfigurationRepository>();
			services.AddScoped<PollRepository>();
			services.AddScoped<PremiumKeyRepository>();
			services.AddScoped<QuoteRepository>();
			services.AddScoped<ReactionRoleRepository>();
			services.AddScoped<ReminderRepository>();
			services.AddScoped<RepRepository>();
			services.AddScoped<RoleRewardRepository>();
			services.AddScoped<ServerExperienceRepository>();
			services.AddScoped<UpvoteRepository>();
			services.AddScoped<WarnConfigurationRepository>();

			// Active polls is a largely static class but needs to be constructed by the IOC. 
			services.AddSingleton<ActivePolls>();
			services.AddSingleton<GuildLoggerService>();
			services.AddSingleton<GreetingService>();
			services.AddSingleton<UpvoteNotifierService>();

			services.AddSingleton<SilentSysActions>();

			services.AddSingleton<AudioQueueLocker>();
			services.AddSingleton<ITimerService, TimerService>();
			services.AddSingleton<IAntiraidService, AntiraidService>();

			services.AddControllers();

			services.AddSingleton(new NekoClient("kaguya-v4"));

			// Osu setup (OsuSharp)
			services.AddSingleton(provider =>
			{
				var adminConfigs = provider.GetRequiredService<IOptions<AdminConfigurations>>();
				var logger = provider.GetRequiredService<ILogger<Startup>>();
				string apiKey = adminConfigs.Value.OsuApiKey;
				if (string.IsNullOrWhiteSpace(apiKey))
				{
					logger.LogWarning("osu! api key not provided! All osu! features will fail on execution!");

					return new OsuClient(new OsuSharpConfiguration
					{
						// Needed so that the bot doesn't encounter an immediate runtime error
						// if an empty API key is provided. OsuSharp doesn't actually check for a valid 
						// API key but will throw an exception if the API key is null/empty.
						ApiKey = "I'M INVALID!!!"
					});
				}

				return new OsuClient(new OsuSharpConfiguration
				{
					ApiKey = adminConfigs.Value.OsuApiKey
				});
			});

			services.AddSingleton(_ =>
			{
				var cs = new CommandService();
				return cs;
			});

			services.AddSingleton(provider =>
			{
				var discordConfigs = provider.GetRequiredService<IOptions<DiscordConfigurations>>();

				var restClient = new DiscordRestClient();
				restClient.LoginAsync(TokenType.Bot, discordConfigs.Value.BotToken).GetAwaiter().GetResult();
				int shards = restClient.GetRecommendedShardCountAsync().GetAwaiter().GetResult();

				var client = new DiscordShardedClient(new DiscordSocketConfig
				{
					AlwaysDownloadUsers = discordConfigs.Value.AlwaysDownloadUsers ?? true,
					MessageCacheSize = discordConfigs.Value.MessageCacheSize ?? 50,
					TotalShards = shards,
					LogLevel = LogSeverity.Debug,
					ExclusiveBulkDelete = true // todo: reflect in guild logger with custom logtype!!
				});

				return client;
			});

			services.AddSingleton(provider =>
			{
				var client = provider.GetRequiredService<DiscordShardedClient>();
				return new InteractivityService(client, TimeSpan.FromMinutes(5));
			});

			services.AddLavaNode(x => { x.SelfDeaf = true; });
			services.AddSingleton<AudioService>();
			services.AddSingleton<AutoRoleService>();

			// CommonEmotes setup
			services.AddSingleton<CommonEmotes>();
			services.AddSingleton<KaguyaEvents>();

			services.AddHostedService<DiscordWorker>();

			// Must be after discord.
			services.AddHostedService<AntiraidWorker>();
			services.AddHostedService<KaguyaPremiumRoleService>();
			services.AddHostedService<ReminderService>();
			services.AddHostedService<RevertAdminActionService>();
			services.AddHostedService<StatusRotationService>();
			services.AddHostedService<StatisticsUploaderService>();
			services.AddHostedService<TimerWorker>();
#if !DEBUG
			services.AddHostedService<TopGgStatsUpdaterService>();
#endif
			services.AddHostedService<UpvoteExpirationService>();
			services.AddHostedService<PollService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
		}
	}
}