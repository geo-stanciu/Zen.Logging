using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Extensions
{
    public static class ExceptionExtensions
    {
        public static string GetMessageWithStackTrace(this Exception ex)
        {
            var sb = new StringBuilder();

            sb.AppendLine(ex.Message);

            if (ex.InnerException != null)
                sb.AppendLine(ex.InnerException.GetMessageWithStackTrace());

            if (!string.IsNullOrEmpty(ex.StackTrace))
                sb.AppendLine(ex.StackTrace);

            return sb.ToString();
        }
    }
}
