using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kaguya.Database.Context;
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

			services.AddDbContextPool<KaguyaDbContext>(builder =>
			{
				builder
					.UseMySql(Configuration.GetConnectionString("Database"),
					          ServerVersion.AutoDetect(Configuration.GetConnectionString("Database")));
			});

			// TODO: Add user repositories, etc.
			// All database repositories are added as scoped here.
			services.AddTransient<KaguyaServerRepository>();
			
			services.AddControllers();

			services.AddSingleton(provider =>
			{
				var cs = new CommandService();
				return cs;
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