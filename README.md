# Egonsoft.HU DependencyInjection Extensions Abstractions

[![GitHub](https://img.shields.io/github/license/gcsizmadia/EgonsoftHU.Extensions.DependencyInjection.Abstractions?label=License)](https://opensource.org/licenses/MIT)
[![Nuget](https://img.shields.io/nuget/v/EgonsoftHU.Extensions.DependencyInjection.Abstractions?label=NuGet)](https://www.nuget.org/packages/EgonsoftHU.Extensions.DependencyInjection.Abstractions)
[![Nuget](https://img.shields.io/nuget/dt/EgonsoftHU.Extensions.DependencyInjection.Abstractions?label=Downloads)](https://www.nuget.org/packages/EgonsoftHU.Extensions.DependencyInjection.Abstractions)

Abstractions for dependency injection.

Commonly Used Types:
- `EgonsoftHU.Extensions.DependencyInjection.IAssemblyRegistry`
- `EgonsoftHU.Extensions.DependencyInjection.DefaultAssemblyRegistry`

## Table of Contents

- [Introduction](#introduction)
- [Releases](#releases)
- [Instructions](#instructions)
- [Example](#example)

## Introduction

The motivation behind this project is to automatically discover and load all _relevant_ assemblies into the current `AppDomain`.

## Releases

You can download the package from [nuget.org](https://www.nuget.org/).
- [EgonsoftHU.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/EgonsoftHU.Extensions.DependencyInjection.Abstractions)

You can find the release notes [here](https://github.com/gcsizmadia/EgonsoftHU.Extensions.DependencyInjection.Abstractions/releases).

## Instructions

***First***, install the *EgonsoftHU.Extensions.DependencyInjection.Abstractions* [NuGet package](https://www.nuget.org/packages/EgonsoftHU.Extensions.DependencyInjection.Abstractions).
```
dotnet add package EgonsoftHU.Extensions.DependencyInjection.Abstractions
```

***Then***, you may configure the logging of the `DefaultAssemblyRegistry` class to use the logging library you configured in your project.

**Please note:**
- The internal dependency to `Serilog` has been removed from the package, hence no log event from `DefaultAssemblyRegistry` is logged by default.
- It is optional to configure logging for `DefaultAssemblyRegistry`.
- There are 2 forms of the message templates:
  - `logEvent.MessageTemplate.Structured` for use with structured logging, e.g. `Serilog`.
  - `logEvent.MessageTemplate.Indexed` for use with `String.Format()`.

*Example configuration for **Serilog**:*

```csharp
using EgonsoftHU.Extensions.DependencyInjection;
using EgonsoftHU.Extensions.Logging;

using Serilog;

ILogger logger = Log.Logger.ForContext<DefaultAssemblyRegistry>();

DefaultAssemblyRegistry.ConfigureLogging(
    logEvent =>
    logger
        .ForContext(PropertyBagEnricher.Create().AddRange(logEvent.Properties))
        .Verbose(logEvent.MessageTemplate.Structured, logEvent.Arguments)
);
```

*Example configuration for **Microsoft.Extensions.Logging**:*

```csharp
using EgonsoftHU.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(
    loggingBuilder =>
    loggingBuilder
        .SetMinimumLevel(LogLevel.Debug)
        .AddJsonConsole(
            options =>
            {
                options.IncludeScopes = true;
                options.JsonWriterOptions = new() { Indented = true };
            }
        )
        .AddDebug()
);

DefaultAssemblyRegistry.ConfigureLogging(
    logEvent =>
    {
        ILogger logger = loggerFactory.CreateLogger<DefaultAssemblyRegistry>();

        using (logger.BeginScope(logEvent.Properties))
        {
            logger.LogDebug(logEvent.MessageTemplate.Structured, logEvent.Arguments);
        }
    },
    LoggingLibrary.MicrosoftExtensionsLogging
);
```

*Example configuration for **System.Console**:*

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EgonsoftHU.Extensions.DependencyInjection;

DefaultAssemblyRegistry.ConfigureLogging(
    logEvent =>
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendFormat(logEvent.MessageTemplate.Indexed, logEvent.Arguments);
        sb.AppendLine();

        int maxKeyLength = logEvent.Properties.Keys.Max(key => key.Length);

        foreach (KeyValuePair<string, object?> property in logEvent.Properties)
        {
            sb.AppendFormat(
                "  [{0}] = [{1}]",
                property.Key.PadRight(maxKeyLength),
                property.Value
            );

            sb.AppendLine();
        }

        Console.WriteLine(sb);
    }
);
```

***Then***, you can initialize an instance of the `DefaultAssemblyRegistry` class by providing the file name prefixes of your assemblies.

```csharp
using EgonsoftHU.Extensions.DependencyInjection;

// This will search for assemblies as below then loads them into the current AppDomain:
//     Root Folder : AppContext.BaseDirectory
//     Pattern #1  : YourCompany.*.dll
//     Pattern #2  : Custom.*.dll
//     SearchOption: SearchOption.AllDirectories

DefaultAssemblyRegistry.Initialize("YourCompany", "Custom");
```

***Finally***, if you need the loaded assemblies then get the current instance of the `DefaultAssemblyRegistry` class.

```csharp
using EgonsoftHU.Extensions.DependencyInjection;

var assemblies = DefaultAssemblyRegistry.Current.GetAssemblies();
```

### Example

Suppose you have an ASP.NET Core project that needs to load controllers from other projects.

Let's create an extension method that will do the magic.

```csharp
using System.Reflection;

using EgonsoftHU.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddApplicationParts(this IMvcBuilder builder)
    {
        // This is the assembly of your ASP.NET Core startup project.
        // We will not call the AddApplicationPart() method for this assembly.
        Assembly entryAssembly = Assembly.GetEntryAssembly();

        DefaultAssemblyRegistry
            .Current
            .GetAssemblies()
            .Where(
                assembly =>
                assembly != entryAssembly
                &&
                assembly.DefinedTypes.Any(typeInfo => typeof(ControllerBase).IsAssignableFrom(typeInfo))
            )
            .ToList()
            .ForEach(assembly => builder.AddApplicationPart(assembly));

        return builder;
    }
}
```

Now you can use it in your `Startup.cs` file.

```csharp
using EgonsoftHU.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Let's initialize the assembly registry.
    DefaultAssemblyRegistry.Initialize("YourCompany", "Custom");

    services
        .AddMvc()
        .AddApplicationParts(); // <-- This will load controllers, view components, or tag helpers from all assemblies other than the entry assembly.
}
```
