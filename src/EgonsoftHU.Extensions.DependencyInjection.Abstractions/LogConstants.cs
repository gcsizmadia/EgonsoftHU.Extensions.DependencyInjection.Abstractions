// Copyright © 2022 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

namespace EgonsoftHU.Extensions.DependencyInjection
{
    internal static class LogConstants
    {
        internal const string SourceContext = nameof(SourceContext);

        internal const string SourceMember = nameof(SourceMember);

        internal const string Unknown = "(unknown)";

        internal static class MicrosoftExtensionsLogging
        {
            internal const string BraceOpen = "{";

            internal const string BraceClose = "}";

            internal const string OriginalFormat = BraceOpen + nameof(OriginalFormat) + BraceClose;

            internal const string SourceMember = BraceOpen + nameof(SourceMember) + BraceClose;
        }
    }
}
