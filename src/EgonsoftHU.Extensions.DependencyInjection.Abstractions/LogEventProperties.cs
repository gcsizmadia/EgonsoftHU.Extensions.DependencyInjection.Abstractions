// Copyright © 2022 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections;
using System.Collections.Generic;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    internal sealed class LogEventProperties : IReadOnlyDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> properties;
        private readonly LoggingLibrary loggingLibrary;

        internal LogEventProperties(Dictionary<string, object?> properties, LoggingLibrary loggingLibrary)
        {
            this.properties = new(properties);
            this.loggingLibrary = loggingLibrary;

            if (IsMicrosoftExtensionsLogging())
            {
                this.properties.Add(
                    LogConstants.MicrosoftExtensionsLogging.OriginalFormat,
                    this.properties.ContainsKey(LogConstants.SourceMember)
                        ? LogConstants.MicrosoftExtensionsLogging.SourceMember
                        : String.Empty
                );
            }
        }

        public override string? ToString()
        {
            if (IsMicrosoftExtensionsLogging())
            {
                return
                    properties.TryGetValue(LogConstants.SourceMember, out object? value) && value is string sourceMember
                        ? sourceMember
                        : String.Empty;
            }

            return base.ToString();
        }

        private bool IsMicrosoftExtensionsLogging()
        {
            return loggingLibrary == LoggingLibrary.MicrosoftExtensionsLogging;
        }

        bool IReadOnlyDictionary<string, object?>.ContainsKey(string key)
        {
            return properties.ContainsKey(key);
        }

        bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
        {
            return properties.TryGetValue(key, out value);
        }

        object? IReadOnlyDictionary<string, object?>.this[string key] => properties[key];

        IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => properties.Keys;

        IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => properties.Values;

        int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => properties.Count;

        IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object?>>)properties).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)properties).GetEnumerator();
        }
    }
}
