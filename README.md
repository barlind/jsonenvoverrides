# JsonEnvOverrides, or why-can't-I-easily-use-arrays-ffs

## Reason for existing

I just want to have a nice appsettings.json with arrays BUT ALSO easily override those arrays in an app service / configuration in azure.

Thus enter **JsonEnvOverrides**. It is a tiny helper for .NET configuration that lets you override **hierarchical settings (including arrays and objects)** using **JSON stored in environment variables**.

It builds on top of the normal `EnvironmentVariablesConfigurationProvider` and doesn’t replace it. You still use `__` to represent `:` in keys – this just makes it easy to paste JSON values.

## Why?

By default, .NET expects you to override arrays and nested objects like this:

```
MyApp__Teams__0 = team1
MyApp__Teams__1 = team2
MyApp__Teams__2 = team3
```

That’s painful to maintain, especially in Azure App Service’s UI.

With JsonEnvOverrides, you can instead do:

```
MyApp__Teams = ["team1","team2","team3"]
```

and the JSON will be expanded into `MyApp:Teams:0`, `MyApp:Teams:1`, etc., so that normal `Options` binding works.

## How it works (in one sentence)

- It scans environment variables whose names start with a given prefix (e.g. `MyApp__`),
- For any whose **value looks like JSON** (starts with `{` or `[`),
- It parses that JSON and expands it into hierarchical keys via an in-memory configuration provider that’s added **last**, so those values win.

## Installation

This is designed as "copy-paste code", not a package.

1. Create a file in your project, e.g. `Configuration/JsonEnvOverridesExtensions.cs`.
2. Paste the contents of the `JsonEnvOverridesExtensions.cs` file into it.
3. Adjust the namespace if you want (e.g. to `MyApp.Configuration`).

## Usage

### 1. appsettings.json

```json
{
  "MyApp": {
    "Teams": [ "team1", "team2" ],
    "Nested": {
      "List": [
        { "Name": "a", "Value": 1 },
        { "Name": "b", "Value": 2 }
      ]
    },
    "SomeFeature": {
      "Enabled": true
    }
  }
}
```

### 2. Program.cs / Startup

```csharp
using JsonEnvOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
  .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
  .AddEnvironmentVariables()
  .AddJsonEnvOverrides("MyApp"); // <-- JSON-in-env expansion for MyApp
```

> Note: `AddJsonEnvOverrides("MyApp")` should come **after** `.AddEnvironmentVariables()` so that the expanded keys overlay the existing configuration.

### 3. Environment / App Service settings

You can now override complex structures with plain JSON:

```
# Override list of strings
MyApp__Teams = ["prod1","prod2","prod3"]

# Override list of objects
MyApp__Nested__List = [
  { "Name": "prod", "Value": 999 }
]

# Override nested object
MyApp__SomeFeature = { "Enabled": false }
```

### 4. Options binding

Define your settings model to match the `MyApp` structure:

```csharp
public class MyAppSettings
{
    public List<string> Teams { get; set; } = new();
    public NestedSettings Nested { get; set; } = new();
    public SomeFeatureSettings SomeFeature { get; set; } = new();
}

public class NestedSettings
{
    public List<ListItem> List { get; set; } = new();
}

public class ListItem
{
    public string Name { get; set; }
    public int Value { get; set; }
}

public class SomeFeatureSettings
{
    public bool Enabled { get; set; }
}

// In Program.cs / Startup:
builder.Services.Configure<MyAppSettings>(
    builder.Configuration.GetSection("MyApp"));
```

Binding works as usual. If there’s a JSON override in env, it wins over the values from `appsettings.json` / `appsettings.{Environment}.json`.

## Behavior notes

- **Which env vars are considered?**\
  Only those whose **name starts with** `rootPrefix + "__"` – for example, if you call `AddJsonEnvOverrides("MyApp")`, it will inspect variables like:

  - `MyApp__Teams`
  - `MyApp__Nested__List`
  - `MyApp__SomeFeature`

- **When is a value treated as JSON?**\
  Only when, after trimming leading whitespace, the value starts with `'{'` or `'['`.

- **What happens on invalid JSON?**\
  By default, the code throws an `InvalidOperationException` on startup and stops processing overrides.\
  If you prefer best-effort behavior, call `AddJsonEnvOverrides("MyApp", continueOnError: true)` and it will **skip only the invalid variable** while still expanding the others.

- **Types:**

  - Strings are stored as strings.
  - Numbers and booleans use `GetRawText()`/`ToString()` so they bind correctly to numeric/bool properties.
  - `null` becomes a `null` config value (the binder will treat it accordingly).

## Example: Azure App Service

In Azure portal → App Service → Configuration → Application settings:

- Add `MyApp__Teams`\
  Value: `["team1-prod","team2-prod","team3-prod"]`

- (Optional) Add `MyApp__SomeFeature`\
  Value: `{ "Enabled": true }`

Restart the app and `IOptions<MyAppSettings>` will reflect those values.

## AI Code Agent Instructions

This repository includes a dedicated guide for AI-based coding tools.

**Any AI code agent must read and follow  
`JsonEnvOverrides-Agent-Guide.md` before making any changes to this project.**

That guide includes:

- The required steps to add JsonEnvOverrides to the project  
- How to wire it into `Program.cs`  
- Build verification rules  
- Optional configuration-refactor workflow  
- Safety rules to avoid unintended changes  

Agents should:

1. Read `JsonEnvOverrides-Agent-Guide.md`  
2. Follow **Stage 1** exactly  
3. Only begin **Stage 2** if the human explicitly approves

## Extras and Community Addons

The core `JsonEnvOverridesExtensions.cs` file is intentionally kept **tiny and dependency-free**.

For more advanced scenarios (custom serializers, extra conventions, etc.), this repo includes an `extras/` folder with **optional helpers**:

- `extras/JsonEnvOverridesExtrasCore.cs` – defines:
  - `JsonEnvOverridesExtraAttribute`
  - `IJsonEnvOverrideExtra`
  - `AddJsonEnvOverridesExtras(...)` for attribute-based discovery
- `extras/community/` – example community addons that build on top of the core.

These files are **not referenced by the core extension**. If you want to use them:

1. Copy the files you need from `extras/` (and/or `extras/community/`) into your project.
2. In `Program.cs` add:

```csharp
using JsonEnvOverrides;
using JsonEnvOverrides.Extras;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddJsonEnvOverrides("MyApp")
    .AddJsonEnvOverridesExtras(); // optional: discovers IJsonEnvOverrideExtra implementations
```

3. Add your own classes under `JsonEnvOverrides.Extras` that implement `IJsonEnvOverrideExtra` and are marked with `[JsonEnvOverridesExtra("Name")]`.

This way the core stays simple, while power users and PRs can share reusable addons under `extras/community` without affecting the base behavior.

## License

MIT License – do whatever you want, just keep the copyright notice.

```
MIT License

Copyright (c) 2025 YOUR NAME

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

