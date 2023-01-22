using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Enums
{
    public enum SyslogLevel
    {
        Emergency = 0,
        Alert = 1,
        Critical,
        Error,
        Warning,
        Notice,
        Info,
        Debug
    }
}
