// Copyright © 2022 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents a log event.
    /// </summary>
    internal sealed class LogEvent<TSourceContext> : ILogEvent
    {
        internal static readonly List<Action<ILogEvent>> LogActions = new();

        private readonly Dictionary<string, object?> properties = new();

        internal LogEvent(ILogMessageTemplate messageTemplate, params object?[] arguments)
        {
            MessageTemplate = messageTemplate;
            Arguments = arguments;

            AddProperty("SourceContext", typeof(TSourceContext).FullName);
        }

        public ILogMessageTemplate MessageTemplate { get; }

        public object?[] Arguments { get; }

        public IReadOnlyDictionary<string, object?> Properties => new ReadOnlyDictionary<string, object?>(properties);

        internal LogEvent<TSourceContext> Here([CallerMemberName] string? sourceMemberName = null)
        {
            return AddProperty("SourceMember", sourceMemberName);
        }

        internal LogEvent<TSourceContext> AddProperty(string key, object? value)
        {
            if (properties.TryGetValue(key, out object? _))
            {
                properties[key] = value;
            }
            else
            {
                properties.Add(key, value);
            }
            
            return this;
        }

        internal void Log()
        {
            LogActions.ForEach(logAction => logAction.Invoke(this));
        }
    }
}
