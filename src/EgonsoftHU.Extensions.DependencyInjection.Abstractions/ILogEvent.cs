// Copyright © 2022-2024 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System.Collections.Generic;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents a log event.
    /// </summary>
    public interface ILogEvent
    {
        /// <summary>
        /// The message template.
        /// </summary>
        public ILogMessageTemplate MessageTemplate { get; }

        /// <summary>
        /// The arguments for the message template.
        /// </summary>
        public object?[] Arguments { get; }

        /// <summary>
        /// Additional log event properties.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Properties { get; }
    }
}
