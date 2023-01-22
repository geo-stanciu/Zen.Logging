using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Zen.Logging.Configuration
{
    public class LoggingServiceConfiguration
    {
        public int EventId { get; set; }

        public Dictionary<LogLevel, ConsoleColor> LogLevels { get; set; } = new()
        {
        };
    }
}
