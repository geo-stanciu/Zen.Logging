using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zen.Logging.Models;

namespace Zen.Logging.Services
{
    public class LoggingQueueService : ILoggingQueueService
    {
        private object _lock = new object();

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
            lock (_lock)
            {
                if (queue.IsAddingCompleted
                    || consoleLoggingQueue.IsAddingCompleted
                    || exceptionLoggingQueue.IsAddingCompleted
                    || fileLoggingQueue.IsAddingCompleted)
                    return;

                if (_loggingConfigModel?.Value?.Loggers?.MessageQueueLogger ?? false)
                    queue.Add(logMessage);

                if (_loggingConfigModel?.Value?.Loggers?.ConsoleLogger ?? true) // we want at least console logging
                    consoleLoggingQueue.Add(logMessage);

                if (_loggingConfigModel?.Value?.Loggers?.FileLogger ?? false)
                    fileLoggingQueue.Add(logMessage);

                if (logMessage.logLevel == LogLevel.Error || !string.IsNullOrEmpty(logMessage.exception_message))
                    exceptionLoggingQueue.Add(logMessage);
            }
        }

        public void SignalEnd()
        {
            lock (_lock)
            {
                queue.CompleteAdding();
                consoleLoggingQueue.CompleteAdding();
                exceptionLoggingQueue.CompleteAdding();
                fileLoggingQueue.CompleteAdding();
            }
        }

        public bool IsDone()
        {
            lock (_lock)
            {
                return
                    queue.IsCompleted
                    && queue.Count == 0
                    && consoleLoggingQueue.IsCompleted
                    && consoleLoggingQueue.Count == 0
                    && exceptionLoggingQueue.IsCompleted
                    && exceptionLoggingQueue.Count == 0
                    && fileLoggingQueue.IsCompleted
                    && fileLoggingQueue.Count == 0;
            }
        }
    }
}
