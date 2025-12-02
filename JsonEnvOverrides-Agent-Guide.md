# Instructions for AI Code Agents: Integrate JsonEnvOverrides into a .NET Project in VS Code

These instructions are for an AI code agent working on a C#/.NET project in **Visual Studio Code**.

## Goal

Add **JsonEnvOverrides** to a .NET project so that **JSON stored in environment variables** can override hierarchical configuration (including arrays and objects) under a chosen root section (e.g. `MyApp`).

The agent **must not** change unrelated behavior.

## High-level Behavior

JsonEnvOverrides will:

1. Scan environment variables whose names start with `<RootPrefix>__` (double underscore), e.g. `MyApp__`.
2. For any such variable whose value looks like JSON (after trimming whitespace, starts with `{` or `[`), parse the JSON.
3. Expand that JSON into hierarchical configuration keys (e.g. `MyApp:Teams:0`, `MyApp:Nested:List:0:Name`) via an in-memory configuration provider added **after** `AddEnvironmentVariables()`.
4. This lets normal options binding continue to work with `services.Configure<Settings>(configuration.GetSection("MyApp"))`.

## Preconditions / Assumptions

- The project is a .NET application (preferably ASP.NET Core) using the modern `WebApplication.CreateBuilder(args)` pattern.
- The project already uses `appsettings.json` and `AddJsonFile(...)` in `Program.cs`.
- The project uses VS Code, but the agent will modify files directly in the filesystem.
- The agent can use `git` and `curl`/`wget` or similar tools in a terminal to download files from GitHub.
- The user will later replace `MyApp` with their own configuration root key if needed.

## 1. Download the Latest JsonEnvOverrides Code from GitHub

1. Use the repository URL provided by the user, for example:
   - `https://github.com/barlind/JsonEnvOverrides`

2. Fetch the latest version of the **extension source file** without modifying the current solution structure. Prefer one of these approaches:

   - **If the repo has a raw link for the extension file** (recommended):
     - Download `JsonEnvOverridesExtensions.cs` using a raw GitHub URL, e.g.:
       - `https://raw.githubusercontent.com/barlind/JsonEnvOverrides/main/JsonEnvOverridesExtensions.cs`
     - Save it into the current project under `Configuration/JsonEnvOverridesExtensions.cs`.

   - **If you need to clone the repo:**
     1. Clone it to a temporary folder (e.g. `./.tmp/json-env-overrides`).
     2. Copy only the extension file (e.g. `JsonEnvOverridesExtensions.cs`) from that repo into the current project under `Configuration/JsonEnvOverridesExtensions.cs`.
     3. Do not keep or commit the temporary clone in the main project; it’s just a source.

3. Ensure the extension file is placed inside the main web/API project (the one containing `Program.cs`).

4. Adjust the **namespace** in the downloaded file as needed:
   - If the project’s root namespace is, for example, `MyCompany.MyApp`, change the namespace of the extension class to something like:
     - `namespace MyCompany.MyApp.Configuration;`

5. Ensure all required `using` directives are present and there are no compile errors in the new file.

> Do **not** rewrite the contents of `JsonEnvOverridesExtensions.cs`. Only adjust the namespace and usings if necessary to make it compile within this project.

## 2. Wire JsonEnvOverrides into Program.cs

1. Open the main `Program.cs` file for the web/API project. Look for the code that creates the builder, similar to:

```csharp
var builder = WebApplication.CreateBuilder(args);
```

2. Locate the configuration builder chain, which typically looks like this:

```csharp
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();
```

3. Modify this block to call `AddJsonEnvOverrides("MyApp")` **after** `AddEnvironmentVariables()`:

```csharp
using MyCompany.MyApp.Configuration; // or the namespace where JsonEnvOverridesExtensions lives

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddJsonEnvOverrides("MyApp");
```

4. At the top of `Program.cs`, add the appropriate `using` statement for the extension method, based on the namespace you set in `JsonEnvOverridesExtensions.cs`. For example:

```csharp
using MyCompany.MyApp.Configuration;
```

5. Do **not** remove or reorder other configuration providers unless strictly necessary. The main requirement is that `AddJsonEnvOverrides("MyApp")` comes **after** `AddEnvironmentVariables()` so the expanded JSON overrides the existing environment values.

## 3. Configuration Root: Using "MyApp" as a Placeholder

- The string `"MyApp"` passed to `AddJsonEnvOverrides("MyApp")` is the **configuration root prefix**.
- The agent should **not** try to infer or rename it automatically.
- Leave it as `"MyApp"` unless explicitly told otherwise.
- The human user can later search and replace `MyApp` with their actual root section name (e.g. `MegaProject`, `MyService`, etc.).

## 4. Example Usage Pattern (for Documentation Only)

The agent may optionally update documentation files (such as `README.md` or `docs/configuration.md`) with an example showing how this works using `MyApp`.

Example configuration in `appsettings.json`:

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

Example env/App Service overrides (documentation only, do not hard-code these in code):

```text
MyApp__Teams = ["prod1","prod2","prod3"]

MyApp__Nested__List = [
  { "Name": "prod", "Value": 999 }
]

MyApp__SomeFeature = { "Enabled": false }
```

Example options binding model (for docs):

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

The agent should not add these classes unless instructed; they are examples to help the human developer.

## 5. Validation Steps

After integrating JsonEnvOverrides, the agent must:

1. Run `dotnet build` from the solution root.
2. Ensure there are **no compilation errors** related to:
   - Missing namespaces
   - Extension method resolution for `AddJsonEnvOverrides`
3. If needed, fix only the errors directly related to JsonEnvOverrides integration.
4. Do **not** refactor or reformat unrelated code.

## 6. Test Strategy (For Agents Working In This Repo)

When this repository itself is opened in VS Code and the agent is modifying it (as opposed to integrating JsonEnvOverrides into another app), prefer the existing test setup instead of ad-hoc code:

- Use the existing test project `tests/JsonEnvOverrides.Tests.csproj`.
- Prefer adding or updating tests in:
  - `tests/JsonEnvOverridesExtensionsTests.cs` for core JSON‑from‑env behavior and integration with `ConfigurationBuilder`.
  - `tests/JsonEnvOverridesExtrasTests.cs` for the extras discovery helpers.
- After changes that may affect behavior, run:

  ```bash
  dotnet test tests/JsonEnvOverrides.Tests.csproj
  ```

Do not introduce a second test framework or duplicate test projects; extend the existing tests instead.

## 7. Things the Agent Must Not Do

- Do not remove or reorder existing configuration providers unless explicitly asked.
- Do not change behavior unrelated to configuration.
- Do not hard-code environment values inside the code.
- Do not rename `MyApp` automatically.
- Do not introduce new dependencies (NuGet packages, projects) unless explicitly instructed.
- Do not automatically wire in any files under the `extras` folder when integrating into another app; they are optional helpers and should only be used if the human developer explicitly asks for them.

---

These instructions are intended to be reusable: you can give them to any AI code agent working on a .NET project in VS Code and have it reliably add JsonEnvOverrides by fetching the latest code from GitHub and wiring it into `Program.cs`. The human developer can then adjust the root section name and settings models as needed.

