// Copyright © 2022 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Reflection;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    internal sealed record AssemblyRegistryEntry
    {
        internal AssemblyRegistryEntry(Assembly assembly)
        {
            Assembly = assembly;
#if NETCOREAPP3_1_OR_GREATER
            Name = assembly.GetName().Name ?? String.Empty;
#else
            Name = assembly.GetName().Name;
#endif
            FullName = Assembly.GetName().FullName;
        }

        internal Assembly Assembly { get; }

        internal string Name { get; }

        internal string FullName { get; }
    }
}
