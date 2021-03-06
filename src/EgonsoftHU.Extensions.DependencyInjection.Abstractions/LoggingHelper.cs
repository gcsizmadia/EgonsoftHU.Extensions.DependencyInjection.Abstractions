// Copyright © 2022 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using Serilog;
using Serilog.Core;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    internal static class LoggingHelper
    {
        private const string OutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fffffff zzz} [{Level:u3}] [{SourceContext}]::[{MemberName}] {Message:lj}{NewLine}{Exception}";

        internal const string Unknown = "(unknown)";

        internal static ILogger GetLogger<T>()
        {
            if (Log.Logger == Logger.None)
            {
                return
                    new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .WriteTo.Console(outputTemplate: OutputTemplate)
                        .WriteTo.Debug(outputTemplate: OutputTemplate)
                        .CreateLogger()
                        .ForContext<T>();
            }
            else
            {
                return Log.Logger.ForContext<T>();
            }
        }
    }
}
