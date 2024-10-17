// Copyright © 2022-2024 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;

namespace EgonsoftHU.Extensions.DependencyInjection.Logging
{
    internal class LogEvent : ILogEvent
    {
        public ILogMessageTemplate MessageTemplate { get; init; } = LogMessageTemplate.Empty;

        public object?[] Arguments { get; init; } = Array.Empty<object>();

        public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
    }
}
