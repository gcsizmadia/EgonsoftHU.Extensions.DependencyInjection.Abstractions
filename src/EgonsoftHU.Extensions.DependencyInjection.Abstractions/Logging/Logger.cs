// Copyright © 2022-2024 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using EgonsoftHU.Extensions.Bcl;

namespace EgonsoftHU.Extensions.DependencyInjection.Logging
{
    internal abstract class Logger
    {
        protected readonly Dictionary<string, object?> Properties = new();

        internal static Logger GetLogger<TSourceContext>()
        {
            return new GenericLogger<TSourceContext>();
        }

        protected Logger(Dictionary<string, object?>? properties)
        {
            Properties =
                properties is null
                    ? new()
                    : new(properties);
        }

        internal abstract Logger Configure(Action<ILogEvent> logAction, LoggingLibrary loggingLibrary);

        internal abstract Logger Here([CallerMemberName] string? callerMemberName = null);

        internal abstract void Log(ILogMessageTemplate messageTemplate, params object?[] args);

        private sealed class GenericLogger<TSourceContext> : Logger
        {
            private static readonly List<Action<ILogEvent>> logActions = new();

            private static LoggingLibrary loggingLibrary = LoggingLibrary.Other;

            internal GenericLogger() : this(null)
            {
                Properties[LogConstants.SourceContext] = TypeHelper.GetTypeName<TSourceContext>();
            }

            internal GenericLogger(Dictionary<string, object?>? properties) : base(properties)
            {
            }

            internal override Logger Configure(Action<ILogEvent> logAction, LoggingLibrary loggingLibrary)
            {
                logActions.Add(logAction);
                GenericLogger<TSourceContext>.loggingLibrary = loggingLibrary;

                return this;
            }

            internal override Logger Here([CallerMemberName] string? callerMemberName = null)
            {
                GenericLogger<TSourceContext> logger = new(Properties);

                logger.Properties[LogConstants.SourceMember] = callerMemberName;

                return logger;
            }

            internal override void Log(ILogMessageTemplate messageTemplate, params object?[] args)
            {
                if (GenericLogger<TSourceContext>.IsMicrosoftExtensionsLogging())
                {
                    Properties[LogConstants.MicrosoftExtensionsLogging.OriginalFormat] =
                        Properties.ContainsKey(LogConstants.SourceMember)
                            ? LogConstants.MicrosoftExtensionsLogging.SourceMember
                            : String.Empty;
                }

                var logEvent = new LogEvent()
                {
                    MessageTemplate = messageTemplate,
                    Arguments = args,
                    Properties = Properties.AsReadOnly()
                };

                logActions.ForEach(logAction => logAction.Invoke(logEvent));
            }

            private static bool IsMicrosoftExtensionsLogging()
            {
                return loggingLibrary == LoggingLibrary.MicrosoftExtensionsLogging;
            }
        }
    }
}
