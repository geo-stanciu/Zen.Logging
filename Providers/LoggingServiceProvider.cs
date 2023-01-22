using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zen.Logging.Configuration;
using Zen.Logging.Models;
using Zen.Logging.Services;

namespace Zen.Logging.Providers
{
    public class LoggingServiceProvider : ILoggerProvider
    {
        private readonly IDisposable? _onChangeToken;
        private LoggingServiceConfiguration _currentConfig;
        private readonly ConcurrentDictionary<string, LoggingService> _loggers = new();
        private readonly ILoggingQueueService _loggingQueueService;
        private IOptions<LoggingSourceConfigModel> _loggingSourceConfigModel;

        public LoggingServiceProvider(
            IOptionsMonitor<LoggingServiceConfiguration> config,
            ILoggingQueueService loggingQueueService,
            IOptions<LoggingSourceConfigModel> loggingSourceConfigModel)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
            _loggingQueueService = loggingQueueService;
            _loggingSourceConfigModel = loggingSourceConfigModel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new LoggingService(_loggingQueueService, _loggingSourceConfigModel, name, GetCurrentConfig));
        }

        private LoggingServiceConfiguration GetCurrentConfig() => _currentConfig;

        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken?.Dispose();
        }
    }
}
