using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Interactivity;
using Kaguya.Database.Context;
using Kaguya.Database.Model;
using Kaguya.Database.Repositories;
using Kaguya.Discord;
using Kaguya.Discord.options;
using Kaguya.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Victoria;

namespace Kaguya
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<AdminConfigurations>(Configuration.GetSection(AdminConfigurations.Position));
			services.Configure<DiscordConfigurations>(Configuration.GetSection(DiscordConfigurations.Position));
			
			services.AddDbContext<KaguyaDbContext>(builder =>
			{
				builder
					.UseMySql(Configuration.GetConnectionString("Database"),
						ServerVersion.AutoDetect(Configuration.GetConnectionString("Database")));
			}, ServiceLifetime.Transient);
			
			// TODO: Add user repositories, etc.
			// All database repositories are added as scoped here.
			services.AddTransient<KaguyaServerRepository>();
			services.AddTransient<KaguyaUserRepository>();
			services.AddTransient<AdminActionRepository>();
			services.AddTransient<WordFilterRepository>();
			services.AddTransient<CommandHistoryRepository>();
			
			services.AddControllers();

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
				
				var _client = new DiscordShardedClient(new DiscordSocketConfig
							                    {
							                        AlwaysDownloadUsers = discordConfigs.Value.AlwaysDownloadUsers ?? true,
							                        MessageCacheSize = discordConfigs.Value.MessageCacheSize ?? 50,
							                        TotalShards = shards,
							                        LogLevel = LogSeverity.Debug
							                    });

				return _client;
			});

			services.AddSingleton(provider =>
			{
				var client = provider.GetRequiredService<DiscordShardedClient>();
				return new InteractivityService(client, TimeSpan.FromMinutes(5));
			});
			
			services.AddHostedService<DiscordWorker>();
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