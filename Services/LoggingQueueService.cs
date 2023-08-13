using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Zen.Logging.Models;

namespace Zen.Logging.Services
{
    public class LoggingQueueService : ILoggingQueueService
    {
        private IOptions<LoggingConfigModel> _loggingConfigModel;

        public LoggingQueueService(IOptions<LoggingConfigModel> loggingConfigModel)
        {
            _loggingConfigModel = loggingConfigModel;
        }

        public CancellationToken cancellationToken { get; set; }

        public BlockingCollection<LogMessageModel> queue { get; set; } = new BlockingCollection<LogMessageModel>();

        public BlockingCollection<LogMessageModel> consoleLoggingQueue { get; set; } = new BlockingCollection<LogMessageModel>();

        public BlockingCollection<LogMessageModel> exceptionLoggingQueue { get; set; } = new BlockingCollection<LogMessageModel>();

        public BlockingCollection<LogMessageModel> fileLoggingQueue { get; set; } = new BlockingCollection<LogMessageModel>();

        public void Add(LogMessageModel logMessage)
        {
            if (queue.IsAddingCompleted
                || consoleLoggingQueue.IsAddingCompleted
                || exceptionLoggingQueue.IsAddingCompleted
                || fileLoggingQueue.IsAddingCompleted)
                return;

            bool consoleLogger = _loggingConfigModel?.Value?.Loggers?.ConsoleLogger ?? false;
            bool messageQueueLogger = _loggingConfigModel?.Value?.Loggers?.MessageQueueLogger ?? false;
            bool fileLogger = _loggingConfigModel?.Value?.Loggers?.FileLogger ?? false;

            // we want at least console logging
            if (!consoleLogger && !messageQueueLogger && !fileLogger)
                consoleLogger = true;

            if (consoleLogger)
                consoleLoggingQueue.Add(logMessage);

            if (messageQueueLogger)
                queue.Add(logMessage);

            if (fileLogger)
                fileLoggingQueue.Add(logMessage);

            if (fileLogger && (logMessage.logLevel == LogLevel.Error || !string.IsNullOrEmpty(logMessage.exception_message)))
                exceptionLoggingQueue.Add(logMessage);
        }
    }
}
