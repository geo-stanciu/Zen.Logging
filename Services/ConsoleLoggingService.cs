using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zen.Logging.Models;

namespace Zen.Logging.Services
{
    public class ConsoleLoggingService : BackgroundService
    {
        private ILoggingQueueService _loggingQueueService;

        public ConsoleLoggingService(ILoggingQueueService loggingQueueService)
        {
            _loggingQueueService = loggingQueueService;
        }

        private Task RunAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested || _loggingQueueService.consoleLoggingQueue.Count > 0)
                {
                    int nrItems = _loggingQueueService.consoleLoggingQueue.Count;
                    
                    if (nrItems == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    if (nrItems > 256)
                        nrItems = 256;

                    List<LogMessageModel> logMessages = new List<LogMessageModel>(nrItems);

                    for (int i = 0; i < nrItems; i++)
                    {
                        if (_loggingQueueService.consoleLoggingQueue.TryTake(out LogMessageModel? msg))
                        {
                            if (msg == null)
                                continue;

                            logMessages.Add(msg);
                        }
                        else
                        {
                            await Task.Delay(10);
                            break;
                        }
                    }

                    if (logMessages.Count == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    foreach (LogMessageModel logModel in logMessages)
                    {
                        ConsoleColor originalColor = Console.ForegroundColor;

                        Console.ForegroundColor = logModel.consoleColor;

                        string utc = logModel.log_time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz");
                        Console.WriteLine($"{utc} [{logModel.eventId,2}: {logModel.logLevel,-12}]");

                        Console.ForegroundColor = originalColor;
                        Console.WriteLine($"     {logModel.logName} - {logModel.message}");

                        if (!string.IsNullOrEmpty(logModel.exception_message))
                            Console.WriteLine(logModel.exception_message);
                    }
                }
            });
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () => {
                Task logggerTask = RunAsync(stoppingToken);

                await logggerTask;
            });
        }
    }
}
