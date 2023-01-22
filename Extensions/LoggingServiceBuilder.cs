using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Zen.Logging.Configuration;
using Zen.Logging.Providers;

namespace Zen.Logging.Extensions
{
    public static class LoggingServiceBuilder
    {
        public static ILoggingBuilder AddLoggingService(
            this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, LoggingServiceProvider>());

            LoggerProviderOptions.RegisterProviderOptions
                <LoggingServiceConfiguration, LoggingServiceProvider>(builder.Services);

            return builder;
        }

        public static ILoggingBuilder AddLoggingService(
            this ILoggingBuilder builder,
            Action<LoggingServiceConfiguration> configure)
        {
            builder.AddLoggingService();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
