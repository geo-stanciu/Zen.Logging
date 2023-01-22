using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                  appSettings,
                  loggingConfigModel)
        {
            _logFileNamePrefix = "exceptions";
            _queue = loggingQueueService.exceptionLoggingQueue;
            _logCleanupSettings = loggingConfigModel?.Value?.LogCleanup?.ExceptionsLog;

        }
    }
}
