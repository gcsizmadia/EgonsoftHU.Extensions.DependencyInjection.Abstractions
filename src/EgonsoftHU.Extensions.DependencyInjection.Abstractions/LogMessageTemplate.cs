// Copyright © 2022 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

namespace EgonsoftHU.Extensions.DependencyInjection
{
    internal sealed class LogMessageTemplate : ILogMessageTemplate
    {
        internal static readonly LogMessageTemplate RegisteredAssemblyCount =
            new(
                "Assemblies registered: {0}",
                "Assemblies registered: {RegisteredAssemblyCount}"
            );

        internal static readonly LogMessageTemplate AssemblyFullName =
            new(
                "Assembly: FullName=[{0}]",
                "Assembly: FullName=[{AssemblyFullName}]"
            );

        internal static readonly LogMessageTemplate AssemblyLocation =
            new(
                "Assembly: Location=[{0}]",
                "Assembly: Location=[{AssemblyLocation}]"
            );

        internal static readonly LogMessageTemplate RequestedAssemblyName =
            new(
                "RequestedAssembly.Name     = [{0}]",
                "RequestedAssembly.Name     = [{AssemblyName}]"
            );

        internal static readonly LogMessageTemplate RequestedAssemblyFullName =
            new(
                "RequestedAssembly.Name     = [{0}]",
                "RequestedAssembly.Name     = [{AssemblyFullName}]"
            );

        internal static readonly LogMessageTemplate ResolvedAssemblyFullName =
            new(
                "ResolvedAssembly.FullName  = [{0}]",
                "ResolvedAssembly.FullName  = [{AssemblyFullName}]"
            );

        internal static readonly LogMessageTemplate ResolvedAssemblyLocation =
            new(
                "ResolvedAssembly.Location  = [{0}]",
                "ResolvedAssembly.Location  = [{AssemblyLocation}]"
            );

        internal static readonly LogMessageTemplate ResolvedAssemblyCodeBase =
            new(
                "ResolvedAssembly.CodeBase  = [{0}]",
                "ResolvedAssembly.CodeBase  = [{AssemblyCodeBase}]"
            );

        internal static readonly LogMessageTemplate LoadedAssemblyFullName =
            new(
                "LoadedAssembly.FullName = [{0}]",
                "LoadedAssembly.FullName = [{AssemblyFullName}]"
            );

        internal static readonly LogMessageTemplate LoadedAssemblyLocation =
            new(
                "LoadedAssembly.Location = [{0}]",
                "LoadedAssembly.Location = [{AssemblyLocation}]"
            );

        internal static readonly LogMessageTemplate LoadedAssemblyCodeBase =
            new(
                "LoadedAssembly.CodeBase = [{0}]",
                "LoadedAssembly.CodeBase = [{AssemblyCodeBase}]"
            );

        private LogMessageTemplate(string indexed, string structured)
        {
            Indexed = indexed;
            Structured = structured;
        }

        public string Indexed { get; }

        public string Structured { get; }
    }
}
