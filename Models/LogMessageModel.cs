using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Models
{
    public class LogMessageModel
    {
        public ConsoleColor consoleColor { get; set; }
        public int eventId { get; set; }
        public LogLevel logLevel { get; set; }
        public string? logName { get; set; }

        public string? source { get; set; } = string.Empty;
        public string? source_version { get; set; } = string.Empty;
        public string? ip { get; set; } = string.Empty;
        public DateTime log_time { get; set; } = DateTime.UtcNow;
        public string? message { get; set; }
        public string? exception_message { get; set; }
    }
}
