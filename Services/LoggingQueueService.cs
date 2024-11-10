using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
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

        public Channel<LogMessageModel> queue { get; set; } = Channel.CreateUnbounded<LogMessageModel>();

        public Channel<LogMessageModel> consoleLoggingQueue { get; set; } = Channel.CreateUnbounded<LogMessageModel>();

        public Channel<LogMessageModel> exceptionLoggingQueue { get; set; } = Channel.CreateUnbounded<LogMessageModel>();

        public Channel<LogMessageModel> fileLoggingQueue { get; set; } = Channel.CreateUnbounded<LogMessageModel>();

        public void Add(LogMessageModel logMessage)
        {
            bool consoleLogger = _loggingConfigModel?.Value?.Loggers?.ConsoleLogger ?? false;
            bool messageQueueLogger = _loggingConfigModel?.Value?.Loggers?.MessageQueueLogger ?? false;
            bool fileLogger = _loggingConfigModel?.Value?.Loggers?.FileLogger ?? false;
            bool exceptionLogger = fileLogger && (logMessage.logLevel == LogLevel.Error || !string.IsNullOrEmpty(logMessage.exception_message));
            bool consoleLoggerSuccess = false;
            bool messageQueueLoggerSuccess = false;
            bool fileLoggerSuccess = false;
            bool exceptionLoggerSuccess = false;
            int maxTries = 10;

            // we want at least console logging
            if (!consoleLogger && !messageQueueLogger && !fileLogger)
                consoleLogger = true;

            while (true)
            {
                if (maxTries <= 0)
                    throw new Exception("Logger error while trying to enqueue the log message");

                try
                {
                    if (consoleLogger && !consoleLoggerSuccess)
                        consoleLoggerSuccess = consoleLoggingQueue.Writer.TryWrite(logMessage);

                    if (messageQueueLogger && !messageQueueLoggerSuccess)
                        messageQueueLoggerSuccess = queue.Writer.TryWrite(logMessage);

                    if (fileLogger && !fileLoggerSuccess)
                        fileLoggerSuccess = fileLoggingQueue.Writer.TryWrite(logMessage);

                    if (exceptionLogger && !exceptionLoggerSuccess)
                        exceptionLoggerSuccess = exceptionLoggingQueue.Writer.TryWrite(logMessage);

                    if ((!consoleLogger || consoleLoggerSuccess)
                        && (!messageQueueLogger || messageQueueLoggerSuccess)
                        && (!fileLogger || fileLoggerSuccess)
                        && (!exceptionLogger || exceptionLoggerSuccess))
                    {
                        break;
                    }
                }
                catch
                {
                    Thread.Sleep(100);
                }
                finally
                {
                    maxTries--;
                }
            }
        }
    }
}
