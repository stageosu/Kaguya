﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Kaguya.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kaguya.Internal.Services.Recurring
{
    public class StatisticsUploaderService : BackgroundService, ITimerReceiver
    {
        private readonly ILogger<StatisticsUploaderService> _logger;
        private readonly ITimerService _timerService;
        private readonly IServiceProvider _serviceProvider;

        public StatisticsUploaderService(ILogger<StatisticsUploaderService> logger, ITimerService timerService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _timerService = timerService;
            _serviceProvider = serviceProvider;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            
            await _timerService.TriggerAtAsync(DateTimeOffset.Now, this);
        }
        
        public async Task HandleTimer(object payload)
        {
            await _timerService.TriggerAtAsync(DateTimeOffset.Now.AddMinutes(1), this);

            using (var scope = _serviceProvider.CreateScope())
            {
                var statsRepo = scope.ServiceProvider.GetRequiredService<KaguyaStatisticsRepository>();
                await statsRepo.PostNewAsync();
                
                _logger.LogDebug("Fresh statistics posted");
            }
        }
    }
}