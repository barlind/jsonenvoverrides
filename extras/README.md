# Extras for JsonEnvOverrides

This folder contains **optional, non-core helpers** that build on top of `JsonEnvOverrides`. They are **not** required for basic usage and are not wired into the main extension by default.

## What lives here

- `JsonEnvOverridesExtrasCore.cs`
  - Defines the attribute `[JsonEnvOverridesExtra]` and the interface `IJsonEnvOverrideExtra`.
  - Adds the discovery helper `AddJsonEnvOverridesExtras(this IConfigurationBuilder builder, params Assembly[]? assemblies)`.
  - Intended for **opt-in** usage in apps that want attribute-based discovery of extras.
- `community/`
  - Community and sample extras that demonstrate how to build on top of the core.
  - Designed to be **copy/paste friendly** into your own project, not shipped as part of the core.

## Using the extras core (opt-in)

If you copy `JsonEnvOverridesExtrasCore.cs` into your app (or reference it via a separate package), you can:

1. Implement an extra:

   ```csharp
   using Microsoft.Extensions.Configuration;
   using JsonEnvOverrides.Extras; // or your chosen namespace

   [JsonEnvOverridesExtra("MyLoggingExtra")]
   public sealed class MyLoggingExtra : IJsonEnvOverrideExtra
   {
       public void Apply(IConfigurationBuilder builder)
       {
           // Add additional configuration, logging, etc.
       }
   }
   ```

2. Enable extras discovery when building configuration:

   ```csharp
   using JsonEnvOverrides.Extras;

   var builder = new ConfigurationBuilder()
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
       .AddEnvironmentVariables()
       .AddJsonEnvOverrides("MyApp");

   // Opt-in: scan for extras in the current assembly (and entry assembly):
   builder.AddJsonEnvOverridesExtras();

   var configuration = builder.Build();
   ```

## Design goals

- Keep the **core** (`JsonEnvOverridesExtensions.cs`) tiny and dependency-free.
- Put optional, more opinionated behavior behind an explicit opt-in (`extras`).
- Make it easy for you (or other contributors) to:
  - Copy individual extras into your own project.
  - Maintain a separate packaging/admin repo without dragging in community samples.

For details about how tests and agents should treat extras, see `JsonEnvOverrides-Agent-Guide.md` in the repo root.
