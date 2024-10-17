// Copyright © 2022-2024 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents a log message template.
    /// </summary>
    public interface ILogMessageTemplate
    {
        /// <summary>
        /// The message template with positional-placeholders for use with <c>String.Format</c>.
        /// </summary>
        string Indexed { get; }

        /// <summary>
        /// The message template with parameters for use with structured logging.
        /// </summary>
        string Structured { get; }
    }
}
