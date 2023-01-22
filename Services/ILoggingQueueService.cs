using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zen.Logging.Models;

namespace Zen.Logging.Services
{
    public interface ILoggingQueueService
    {
        BlockingCollection<LogMessageModel> queue { get; set; }
        BlockingCollection<LogMessageModel> consoleLoggingQueue { get; set; }
        BlockingCollection<LogMessageModel> exceptionLoggingQueue { get; set; }
        BlockingCollection<LogMessageModel> fileLoggingQueue { get; set; }
        void Add(LogMessageModel logMessage);
    }
}
