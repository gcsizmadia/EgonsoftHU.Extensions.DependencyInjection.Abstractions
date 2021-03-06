// Copyright © 2022 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using EgonsoftHU.Extensions.Bcl;
using EgonsoftHU.Extensions.Logging.Serilog;

using Serilog;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Default implementation of the <see cref="IAssemblyRegistry"/> interface.
    /// </summary>
    public class DefaultAssemblyRegistry : IAssemblyRegistry
    {
        private static readonly string[] separator = new[] { ", " };

        private static IAssemblyRegistry? current;

        private readonly ILogger logger = LoggingHelper.GetLogger<DefaultAssemblyRegistry>();

        private readonly Dictionary<string, AssemblyRegistryEntry> assemblies = new();

        private readonly IReadOnlyCollection<string> assemblyFileNamePrefixes;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAssemblyRegistry"/> class with the specified assembly file name prefixes.
        /// </summary>
        /// <param name="assemblyFileNamePrefixes">The prefixes of the assembly file names.</param>
        public static IAssemblyRegistry Initialize(params string[] assemblyFileNamePrefixes)
        {
            current = new DefaultAssemblyRegistry(assemblyFileNamePrefixes);

            return current;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAssemblyRegistry"/> class with the specified assembly file name prefixes.
        /// </summary>
        /// <param name="assemblyFileNamePrefixes">The prefixes of the assembly file names.</param>
        public DefaultAssemblyRegistry(params string[] assemblyFileNamePrefixes)
        {
            this.assemblyFileNamePrefixes = new List<string>(assemblyFileNamePrefixes).AsReadOnly();

            AppDomain
                .CurrentDomain
                .GetAssemblies()
                .ToList()
                .ForEach(RegisterAssembly);

            RegisterAssembly(Assembly.GetEntryAssembly());

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            LoadAssemblies();
        }

        /// <summary>
        /// Gets the current instance of the <see cref="DefaultAssemblyRegistry"/> class.
        /// </summary>
        public static IAssemblyRegistry? Current => current;

        /// <inheritdoc/>
        public IEnumerable<Assembly> GetAssemblies()
        {
            ILogger logger = this.logger.Here();

            Assembly[] relevantAssemblies =
                assemblies
                    .Select(kvp => kvp.Value.Assembly)
                    .OrderBy(assembly => assembly.FullName)
                    .ToArray();

            logger.Verbose("Assemblies registered: {RegisteredAssemblyCount}", relevantAssemblies.Length);

            foreach (Assembly assembly in relevantAssemblies)
            {
                logger.Verbose(
                    "Assembly: FullName=[{AssemblyFullName}] Location=[{AssemblyLocation}]",
                    assembly.FullName,
                    assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LoggingHelper.Unknown)
                );
            }

            return relevantAssemblies;
        }

        private void RegisterAssembly(Assembly? assembly)
        {
            if (assembly is null || !IsRelevantAssembly(assembly))
            {
                return;
            }

            var assemblyRegistryEntry = new AssemblyRegistryEntry(assembly);

            if (!assemblies.TryGetValue(assemblyRegistryEntry.Name, out AssemblyRegistryEntry _))
            {
                assemblies[assemblyRegistryEntry.Name] = assemblyRegistryEntry;
            }
        }

        private bool IsRelevantAssembly(Assembly assembly)
        {
            return
                assemblyFileNamePrefixes.Any(
                    assemblyFileNamePrefix => Path.GetFileName(assembly.SafeGetLocation())?.StartsWith(assemblyFileNamePrefix) ?? false
                );
        }

        private void LoadAssemblies()
        {
            IEnumerable<Assembly> alreadyLoadedRelevantAssemblies = GetAssemblies();

            var assemblyFileNames =
                assemblyFileNamePrefixes
                    .SelectMany(
                        assemblyFileNamePrefix =>
                        Directory.GetFiles(
                            AppContext.BaseDirectory,
                            $"{assemblyFileNamePrefix}.*.dll",
                            SearchOption.AllDirectories
                        )
                    )
                    .Where(
                        assemblyFileName =>
                        !alreadyLoadedRelevantAssemblies.Any(
                            assembly =>
                            String.Equals(
                                Path.GetFileName(assemblyFileName),
                                Path.GetFileName(assembly.SafeGetLocation()),
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    .ToList();

            assemblyFileNames.ForEach(assemblyFileName => LoadAssembly(assemblyFileName));
        }

        private Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            ILogger logger = this.logger.Here();

#if NETCOREAPP3_1
            string requestedAssemblyFullName = args.Name ?? String.Empty;
#else
            string requestedAssemblyFullName = args.Name;
#endif

            string requestedAssemblyName =
                requestedAssemblyFullName
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .First();

            logger.Verbose("RequestedAssembly.Name     = [{AssemblyName}]", requestedAssemblyName);
            logger.Verbose("RequestedAssembly.FullName = [{AssemblyFullName}]", requestedAssemblyFullName);

            Assembly? assembly = null;

            if (assemblies.TryGetValue(requestedAssemblyName, out AssemblyRegistryEntry? assemblyRegistryEntry))
            {
                assembly = assemblyRegistryEntry.Assembly;
            }
            else
            {
                string[] assemblyFileNames = FindAssemblyFiles(requestedAssemblyName);

                if (assemblyFileNames.Any())
                {
                    assembly = LoadAssembly(assemblyFileNames.First());
                }
            }

            logger.Verbose("ResolvedAssembly.FullName  = [{AssemblyFullName}]", assembly?.FullName ?? LoggingHelper.Unknown);
            logger.Verbose("ResolvedAssembly.Location  = [{AssemblyLocation}]", assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LoggingHelper.Unknown));
#if NETFRAMEWORK
            logger.Verbose("ResolvedAssembly.CodeBase  = [{AssemblyCodeBase}]", assembly.SafeGetCodeBase().DefaultIfNullOrWhiteSpace(LoggingHelper.Unknown));
#else
            logger.Verbose("ResolvedAssembly.CodeBase  = [{AssemblyCodeBase}]", assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LoggingHelper.Unknown));
#endif

            return assembly;
        }

        private void CurrentDomain_AssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            ILogger logger = this.logger.Here();

            Assembly assembly = args.LoadedAssembly;

            logger.Verbose("LoadedAssembly.FullName = [{AssemblyFullName}]", assembly?.FullName ?? LoggingHelper.Unknown);
            logger.Verbose("LoadedAssembly.Location = [{AssemblyLocation}]", assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LoggingHelper.Unknown));
#if NETFRAMEWORK
            logger.Verbose("LoadedAssembly.CodeBase = [{AssemblyCodeBase}]", assembly.SafeGetCodeBase().DefaultIfNullOrWhiteSpace(LoggingHelper.Unknown));
#else
            logger.Verbose("LoadedAssembly.CodeBase = [{AssemblyCodeBase}]", assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LoggingHelper.Unknown));
#endif

            RegisterAssembly(assembly);
        }

        private static string[] FindAssemblyFiles(string assemblyShortName)
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyShortName}.dll", SearchOption.AllDirectories);
        }

        private static Assembly LoadAssembly(string assemblyFileName)
        {
            if (File.Exists(assemblyFileName))
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(assemblyFileName);
                    var assembly = Assembly.Load(assemblyName);
                    Type[] types = assembly.GetTypes();

                    return assembly;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var exceptions = new List<Exception>()
                    {
                        new FileLoadException($"Load failed for assembly: [{assemblyFileName}]", assemblyFileName)
                    };

                    exceptions.AddRange(ex.LoaderExceptions.Where(loaderEx => loaderEx is not null).Cast<Exception>());

                    throw new AggregateException(exceptions);
                }
                catch (Exception ex)
                {
                    throw new FileLoadException($"Load failed for assembly: [{assemblyFileName}]", assemblyFileName, ex);
                }
            }
            else
            {
                throw new FileNotFoundException($"Assembly not found: [{assemblyFileName}]", assemblyFileName);
            }
        }
    }
}
