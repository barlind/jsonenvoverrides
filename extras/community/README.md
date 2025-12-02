# JsonEnvOverrides Community Extras

This folder is for **optional, community-maintained addons** that build on top of the extras core. See `extras/README.md` for an overview of the extras infrastructure.

## Conventions for community addons

Each addon should:

1. Live in its own `.cs` file under `extras/community/`.
2. Use the `JsonEnvOverrides.Extras` namespace (or a child namespace).
3. Implement `IJsonEnvOverrideExtra` and be marked with `[JsonEnvOverridesExtra("SomeName")]` so it can be discovered by `AddJsonEnvOverridesExtras`.
4. Include a short XML doc comment or a comment block at the top explaining:
    - What it does
    - Any assumptions (e.g. requires `continueOnError = true`, specific env naming, etc.)
    - Example usage in `Program.cs`.

## Using community addons

1. **Copy the file** you want from `extras/community/` into your own project.
2. Make sure you also have the extras core (`JsonEnvOverridesExtrasCore.cs`) in your project, or reference a package that provides it.
3. Reference the namespace, e.g.:

```csharp
using JsonEnvOverrides;
using JsonEnvOverrides.Extras;
```

4. In your configuration pipeline, call the core extension first and then the extras discovery (as described in `extras/README.md`).

> Note: `AddJsonEnvOverridesExtras()` is **optional**. If you don't copy any extras or don't call it, nothing changes.

## Contributing an addon

When submitting a PR that adds a new community addon:

- Put the code under `extras/community/YourAddonName.cs`.
- Add a short section to this README or to the main `README.md` documenting what it does and how to use it.
- Keep dependencies minimal; prefer using only .NET built-ins.
