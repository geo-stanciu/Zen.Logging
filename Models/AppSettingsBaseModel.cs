using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Models;

public class AppSettingsBaseModel
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string ApiUsername { get; set; } = string.Empty;
    public string ApiPassword { get; set; } = string.Empty;

    public string ExceptionLoggingDirectory { get; set; } = string.Empty;
    public string LoggingDirectory { get; set; } = string.Empty;
}
