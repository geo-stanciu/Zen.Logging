using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Models
{
    public class LogCleanupSettingsModel
    {
        public int KeepDays { get; set; }
        public bool ArchiveLogFiles { get; set; }
    }
}
