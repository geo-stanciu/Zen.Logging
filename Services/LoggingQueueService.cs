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
        private object _lock = new object();
        private DateTime _lastMessagesWrittenInMessageQueueCleanupDate = DateTime.Now;

        private IOptions<LoggingConfigModel> _loggingConfigModel;
        private ConcurrentDictionary<string, DateTime> _messagesWrittenInMessageQueue = new ConcurrentDictionary<string, DateTime>();

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

            // we want at least console logging
            bool consoleLogger = _loggingConfigModel?.Value?.Loggers?.ConsoleLogger ?? true;
            bool messageQueueLogger = _loggingConfigModel?.Value?.Loggers?.MessageQueueLogger ?? false;
            bool fileLogger = _loggingConfigModel?.Value?.Loggers?.FileLogger ?? false;

            if (consoleLogger)
                consoleLoggingQueue.Add(logMessage);

            bool isRepeatedMessage = MessageOccurredInTheLastSecond(logMessage);

            if (messageQueueLogger && !isRepeatedMessage)
                queue.Add(logMessage);

            if (fileLogger)
                fileLoggingQueue.Add(logMessage);

            if (fileLogger && (logMessage.logLevel == LogLevel.Error || !string.IsNullOrEmpty(logMessage.exception_message)))
                exceptionLoggingQueue.Add(logMessage);

            CleanupOldMessages();
        }

        private bool MessageOccurredInTheLastSecond(LogMessageModel logMessage)
        {
            string messagehash = GetSha256Hash(
                $"{logMessage.source}" 
                + $"{logMessage.logLevel}" 
                + logMessage.message 
                + logMessage.exception_message ?? ""
            );

            DateTime now = DateTime.Now;
            bool isRepeatedMessage = false;

            if (_messagesWrittenInMessageQueue.TryGetValue(messagehash, out DateTime lastTimeThisMessageOccurred))
            {
                if (lastTimeThisMessageOccurred >= now.AddSeconds(-1))
                    isRepeatedMessage = true;
            }

            if (!isRepeatedMessage)
                _messagesWrittenInMessageQueue.AddOrUpdate(messagehash, now, (k, oldValue) => now);

            return true;
        }

        private void CleanupOldMessages()
        {
            lock (_lock)
            {
                if (_lastMessagesWrittenInMessageQueueCleanupDate < DateTime.Now.AddMinutes(-5))
                {
                    _lastMessagesWrittenInMessageQueueCleanupDate = DateTime.Now;
                }

                _ = Task.Run(() =>
                {
                    for (int i = _messagesWrittenInMessageQueue.Count; i >= 0; i++)
                    {
                        KeyValuePair<string, DateTime> pair = _messagesWrittenInMessageQueue.ElementAt(i);

                        if (pair.Value < DateTime.Now.AddMinutes(-10))
                            _messagesWrittenInMessageQueue.TryRemove(pair.Key, out _);
                    }
                });
            }
        }

        private string GetSha256Hash(string str)
        {
            using var sha256 = SHA256.Create();
            byte[] passBytes = Encoding.UTF8.GetBytes(str);

            var hashValue = sha256.ComputeHash(passBytes);
            StringBuilder sb = new StringBuilder();

            foreach (byte x in hashValue)
                sb.Append(String.Format("{0:x2}", x));

            return sb.ToString();
        }
    }
}
