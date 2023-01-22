using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
using Zen.Logging.Models;

namespace Zen.Logging.Services
{
    public abstract class BaseFileLoggerService : BackgroundService
    {
        protected ILoggingQueueService _loggingQueueService;
        protected IOptions<AppSettingsBaseModel>? _appSettings;
        protected IOptions<LoggingConfigModel>? _loggingConfigModel;

        protected string? _logFileNamePrefix;
        protected BlockingCollection<LogMessageModel>? _queue;
        protected LogCleanupSettingsModel? _logCleanupSettings;

        private static object _deleteOldLogsLockObject = new object();

        public BaseFileLoggerService(
            ILoggingQueueService loggingQueueService,
            IOptions<AppSettingsBaseModel> appSettings,
            IOptions<LoggingConfigModel> loggingConfigModel)
        {
            _loggingQueueService = loggingQueueService;
            _appSettings = appSettings;
            _loggingConfigModel = loggingConfigModel;
        }

        private string GetLoggingDirectoryName(string date)
        {
            return _appSettings?.Value?.LoggingDirectory?
                .Replace("%AppData%", Utils.Util.GetAppDataDirectory())?
                .Replace("%Date%", date) ?? "./";
        }

        private string GetLoggingFileName(string directory, ref string fileSeq, ref int fileNumber)
        {
            string fileName = Path.Combine(directory, $"{_logFileNamePrefix}{fileSeq}.txt");

            if (!File.Exists(fileName))
                return fileName;

            FileInfo fi = new FileInfo(fileName);

            if (fi.Length < 5000000)
                return fileName;

            fileNumber++;
            fileSeq = $"_{fileNumber:0000}";

            return Path.Combine(directory, $"{_logFileNamePrefix}{fileSeq}.txt");
        }

        private void DeleteOldLogs(int keepDays, CancellationToken stoppingToken)
        {
            string today = $"{DateTime.Now.ToUniversalTime():yyyy-MM-dd}";
            string loggingPath = new DirectoryInfo(GetLoggingDirectoryName(today))?.Parent?.FullName ?? "";

            if (string.IsNullOrEmpty(loggingPath))
                return;

            DateTime cutOff = DateTime.Now.AddDays(-1 * keepDays);

            string[] logDirectories = Directory.GetDirectories(loggingPath);

            foreach (string logDirectory in logDirectories)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                string logDirectoryName = new DirectoryInfo(logDirectory).Name;

                if (logDirectoryName == today)
                    continue;
                else if (DateTime.ParseExact(logDirectoryName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) >= cutOff.Date)
                    continue;

                Directory.Delete(logDirectory, true);
            }
        }

        private void ArchiveOldLogFiles(CancellationToken stoppingToken)
        {
            string today = $"{DateTime.Now.ToUniversalTime():yyyy-MM-dd}";
            string loggingPath = new DirectoryInfo(GetLoggingDirectoryName(today))?.Parent?.FullName ?? "";

            if (string.IsNullOrEmpty(loggingPath))
                return;

            string[] logDirectories = Directory.GetDirectories(loggingPath);

            foreach (string logDirectory in logDirectories)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                string logDirectoryName = new DirectoryInfo(logDirectory).Name;

                if (logDirectoryName == today)
                    continue;

                string[] logFiles = Directory.GetFiles(logDirectory, $"{_logFileNamePrefix}*.txt");

                if (logFiles.Length == 0)
                    continue;

                CreateLogArchive(logFiles, logDirectory, logDirectoryName);
                DeleteLogFiles(logFiles);
            }
        }

        private void DeleteLogFiles(string[] logFiles)
        {
            foreach (string logFile in logFiles)
            {
                File.Delete(logFile);
            }
        }

        private void CreateLogArchive(string[] logFiles, string logDirectory, string logDirectoryName)
        {
            string archiveName = Path.Combine(logDirectory, $"{logDirectoryName}.zip");

            using (FileStream zipStream = new FileStream(archiveName, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (string logFile in logFiles)
                    {
                        archive.CreateEntryFromFile(logFile, new FileInfo(logFile).Name);
                    }
                }
            }
        }

        private Task LogCleanupAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                if (_logCleanupSettings == null || (_logCleanupSettings.KeepDays <= 0 && !_logCleanupSettings.ArchiveLogFiles))
                    return;

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_logCleanupSettings.KeepDays > 0)
                    {
                        lock (_deleteOldLogsLockObject)
                        {
                            DeleteOldLogs(_logCleanupSettings.KeepDays, stoppingToken);
                        }
                    }

                    if (_logCleanupSettings.ArchiveLogFiles)
                    {
                        lock (_deleteOldLogsLockObject)
                        {
                            ArchiveOldLogFiles(stoppingToken);
                        }
                    }

                    await Task.Delay(3600 * 1000, stoppingToken);
                }
            });
        }

        private Task LogAsync(CancellationToken stoppingToken)
        {
            if (_queue == null)
                throw new NullReferenceException(nameof(_queue));

            return Task.Run(async () =>
            {
                string lastDay = string.Empty;
                string loggingDirectory = string.Empty;
                int fileNumber = 0;
                string fileSeq = $"_{fileNumber:0000}";

                while (!stoppingToken.IsCancellationRequested || _queue.Count > 0)
                {
                    int nrItems;

                    if (!_queue.TryGetNonEnumeratedCount(out nrItems) || nrItems == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    if (nrItems > 256)
                        nrItems = 256;

                    List<LogMessageModel> messages = new List<LogMessageModel>(nrItems);

                    for (int i = 0; i < nrItems; i++)
                    {
                        if (_queue.TryTake(out LogMessageModel? msg))
                        {
                            if (msg == null)
                                continue;

                            messages.Add(msg);
                        }
                        else
                        {
                            await Task.Delay(10);
                            break;
                        }
                    }

                    if (messages.Count == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    string today = $"{DateTime.Now.ToUniversalTime():yyyy-MM-dd}";

                    if (today != lastDay)
                    {
                        lastDay = today;
                        loggingDirectory = GetLoggingDirectoryName(today);
                        Utils.Util.CreateDirectoryIfNotExists(loggingDirectory);
                    }

                    string fileName = GetLoggingFileName(loggingDirectory, ref fileSeq, ref fileNumber);

                    StringBuilder sb = new StringBuilder();

                    foreach (LogMessageModel logModel in messages)
                    {
                        string utc = logModel.log_time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz");
                        sb.AppendLine($"{utc} [{logModel.eventId,2}: {logModel.logLevel,-12}]");
                        sb.AppendLine($"     {logModel.logName} - {logModel.message}");

                        if (!string.IsNullOrEmpty(logModel.exception_message))
                            sb.AppendLine(logModel.exception_message);
                    }

                    await File.AppendAllTextAsync(fileName, sb.ToString());

                    messages.Clear();
                    await Task.Delay(100);
                }
            });
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () => {
                Task logCleanupTask = LogCleanupAsync(stoppingToken);
                Task logggerTask = LogAsync(stoppingToken);

                await logggerTask;
                await logCleanupTask;
            });
        }
    }
}
