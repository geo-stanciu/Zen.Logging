using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Zen.Logging.Models;

namespace Zen.Logging.Services
{
    public class FileLoggerService : BaseFileLoggerService
    {
        public FileLoggerService(
            ILoggingQueueService loggingQueueService,
            IOptions<AppSettingsBaseModel> appSettings,
            IOptions<LoggingConfigModel> loggingConfigModel)
            : base(
                  loggingQueueService,
                  loggingConfigModel)
        {

            _logFileNamePrefix = "log";
            _logDirectory = appSettings?.Value?.LoggingDirectory;
            _queue = _loggingQueueService.fileLoggingQueue;
            _logCleanupSettings = _loggingConfigModel?.Value?.LogCleanup?.Log;
        }
    }
}
