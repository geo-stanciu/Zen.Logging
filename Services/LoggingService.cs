using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Zen.Logging.Configuration;
using Zen.Logging.Extensions;
using Zen.Logging.Models;
using Zen.Logging.Utils;

namespace Zen.Logging.Services
{
    public class LoggingService : ILogger
    {
        private readonly string _name;
        private readonly Func<LoggingServiceConfiguration> _getCurrentConfig;
        private readonly ILoggingQueueService _loggingQueueService;
        private IOptions<LoggingSourceConfigModel> _loggingSourceConfigModel;

        private string _ipaddress = string.Empty;

        public LoggingService(
            ILoggingQueueService loggingQueueService,
            IOptions<LoggingSourceConfigModel> loggingSourceConfigModel,
            string name,
            Func<LoggingServiceConfiguration> getCurrentConfig)
        {
            _loggingQueueService = loggingQueueService;
            _loggingSourceConfigModel = loggingSourceConfigModel;
            _name = name;
            _getCurrentConfig = getCurrentConfig;

            _ipaddress = Util.GetLocalIPv4();
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) =>
            _getCurrentConfig().LogLevels.ContainsKey(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            LoggingServiceConfiguration config = _getCurrentConfig();

            if (config.EventId == 0 || config.EventId == eventId.Id)
            {
                LogMessageModel logMessage = new LogMessageModel
                {
                    source = _loggingSourceConfigModel.Value?.DefaultSource?.Name ?? string.Empty,
                    source_version = _loggingSourceConfigModel.Value?.DefaultSource?.Version ?? string.Empty,
                    consoleColor = config.LogLevels[logLevel],
                    eventId = eventId.Id,
                    logLevel = logLevel,
                    logName = _name,
                    message = formatter(state, exception),
                    exception_message = exception != null ? exception.GetMessageWithStackTrace() : null,
                    ip = _ipaddress
                };

                _loggingQueueService.Add(logMessage);
            }
        }
    }
}
