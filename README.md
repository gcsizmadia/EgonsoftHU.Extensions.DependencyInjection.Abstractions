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

***Then***, you can initialize an instance of the `DefaultAssemblyRegistry` class by providing the file name prefixes of your assemblies.

```C#
using EgonsoftHU.Extensions.DependencyInjection;

// This will search for assemblies as below then loads them into the current AppDomain:
//     Root Folder : AppContext.BaseDirectory
//     Pattern #1  : YourCompany.*.dll
//     Pattern #2  : Custom.*.dll
//     SearchOption: SearchOption.AllDirectories

DefaultAssemblyRegistry.Initialize("YourCompany", "Custom");
```

***Finally***, if you need the loaded assemblies then get the current instance of the `DefaultAssemblyRegistry` class.

```C#
using EgonsoftHU.Extensions.DependencyInjection;

var assemblies = DefaultAssemblyRegistry.Current.GetAssemblies();
```

### Example

Suppose you have an ASP.NET Core project that needs to load controllers from other projects.

Let's create an extension method that will do the magic.

```C#
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

```C#
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
