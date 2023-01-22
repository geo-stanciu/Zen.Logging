using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zen.Logging.Enums;

namespace Zen.Logging.Factories
{
    public class LogLevelFactory
    {
        private SyslogLevel _level;

        public LogLevelFactory(SyslogLevel level)
        {
            _level = level;
        }

        public LogLevel Build()
        {
            switch (_level)
            {
                case SyslogLevel.Alert:
                    return LogLevel.Warning;
                case SyslogLevel.Critical:
                    return LogLevel.Critical;
                case SyslogLevel.Debug:
                    return LogLevel.Debug;
                case SyslogLevel.Emergency:
                    return LogLevel.Critical;
                case SyslogLevel.Error:
                    return LogLevel.Error;
                case SyslogLevel.Info:
                    return LogLevel.Information;
                case SyslogLevel.Notice:
                    return LogLevel.Warning;
                case SyslogLevel.Warning:
                    return LogLevel.Warning;
                default:
                    return LogLevel.Information;
            }
        }
    }
}
