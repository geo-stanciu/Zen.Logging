using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Zen.Logging.Models;

namespace Zen.Logging.Services
{
    public class ExceptionFileLoggerService : BaseFileLoggerService
    {
        public ExceptionFileLoggerService(ILoggingQueueService loggingQueueService,
            IOptions<AppSettingsBaseModel> appSettings,
            IOptions<LoggingConfigModel> loggingConfigModel)
            : base(
                  loggingQueueService,
                  loggingConfigModel)
        {
            _logFileNamePrefix = "exceptions";
            _logDirectory = appSettings?.Value?.ExceptionLoggingDirectory;
            _queue = loggingQueueService.exceptionLoggingQueue;
            _logCleanupSettings = loggingConfigModel?.Value?.LogCleanup?.ExceptionsLog;

        }
    }
}
