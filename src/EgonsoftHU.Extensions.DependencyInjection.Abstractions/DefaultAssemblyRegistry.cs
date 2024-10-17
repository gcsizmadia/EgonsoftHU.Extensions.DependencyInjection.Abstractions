// Copyright © 2022-2024 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

using EgonsoftHU.Extensions.Bcl;
using EgonsoftHU.Extensions.DependencyInjection.Logging;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Default implementation of the <see cref="IAssemblyRegistry"/> interface.
    /// </summary>
    public sealed class DefaultAssemblyRegistry : IAssemblyRegistry
    {
        private static readonly string[] separator = new[] { ", " };

        private static readonly Logger Logger = Logger.GetLogger<DefaultAssemblyRegistry>();

        private readonly Dictionary<string, AssemblyRegistryEntry> assemblies;

        private readonly IReadOnlyCollection<string> assemblyFileNamePrefixes;

        /// <summary>
        /// Adds a delegate that will be used for logging. This may be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="logAction">The log action.</param>
        /// <param name="loggingLibrary">The logging library used in the <paramref name="logAction"/>. By default, <see cref="LoggingLibrary.Other"/>.</param>
        public static void ConfigureLogging(Action<ILogEvent> logAction, LoggingLibrary? loggingLibrary = LoggingLibrary.Other)
        {
            logAction.ThrowIfNull();

            Logger.Configure(logAction, loggingLibrary.GetValueOrDefault());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAssemblyRegistry"/> class with the specified assembly file name prefixes.
        /// </summary>
        /// <param name="assemblyFileNamePrefixes">The prefixes of the assembly file names.</param>
        [MemberNotNull(nameof(Current))]
        public static IAssemblyRegistry Initialize(params string[] assemblyFileNamePrefixes)
        {
            Current = new DefaultAssemblyRegistry(assemblyFileNamePrefixes);

            return Current;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAssemblyRegistry"/> class with the specified assembly file name prefixes.
        /// </summary>
        /// <param name="assemblyFileNamePrefixes">The prefixes of the assembly file names.</param>
        private DefaultAssemblyRegistry(params string[] assemblyFileNamePrefixes)
        {
            assemblies = new Dictionary<string, AssemblyRegistryEntry>();

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
        public static IAssemblyRegistry? Current { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<Assembly> GetAssemblies()
        {
            Logger logger = Logger.Here();

            Assembly[] relevantAssemblies =
                assemblies
                    .Select(kvp => kvp.Value.Assembly)
                    .OrderBy(assembly => assembly.FullName)
                    .ToArray();

            logger.Log(LogMessageTemplate.RegisteredAssemblyCount, relevantAssemblies.Length);

            foreach (Assembly assembly in relevantAssemblies)
            {
                logger.Log(LogMessageTemplate.AssemblyFullName, GetAssemblyFullName(assembly));
                logger.Log(LogMessageTemplate.AssemblyLocation, assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LogConstants.Unknown));
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
            Logger logger = Logger.Here();

            string requestedAssemblyFullName = args.Name;

            string requestedAssemblyName =
                requestedAssemblyFullName
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .First();

            logger.Log(LogMessageTemplate.RequestedAssemblyName, requestedAssemblyName);
            logger.Log(LogMessageTemplate.RequestedAssemblyFullName, requestedAssemblyFullName);

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

            logger.Log(LogMessageTemplate.ResolvedAssemblyFullName, GetAssemblyFullName(assembly));
            logger.Log(LogMessageTemplate.ResolvedAssemblyLocation, assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LogConstants.Unknown));

            logger.Log(
                LogMessageTemplate.ResolvedAssemblyCodeBase,
                assembly
#if NETFRAMEWORK
                    .SafeGetCodeBase()
#else
                    .SafeGetLocation()
#endif
                    .DefaultIfNullOrWhiteSpace(LogConstants.Unknown)
            );

            return assembly;
        }

        private void CurrentDomain_AssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            Logger logger = Logger.Here();

            Assembly assembly = args.LoadedAssembly;

            logger.Log(LogMessageTemplate.LoadedAssemblyFullName, GetAssemblyFullName(assembly));
            logger.Log(LogMessageTemplate.LoadedAssemblyLocation, assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LogConstants.Unknown));

            logger.Log(
                LogMessageTemplate.LoadedAssemblyCodeBase,
                assembly
#if NETFRAMEWORK
                    .SafeGetCodeBase()
#else
                    .SafeGetLocation()
#endif
                    .DefaultIfNullOrWhiteSpace(LogConstants.Unknown)
            );

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
