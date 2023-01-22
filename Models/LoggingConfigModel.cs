using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Models
{
    public class LoggingConfigModel
    {
        public LogCleanupModel? LogCleanup { get; set; }
        public LoggersConfigModel? Loggers { get; set; }
    }
}
