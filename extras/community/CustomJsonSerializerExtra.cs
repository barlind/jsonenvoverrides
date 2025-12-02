using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace JsonEnvOverrides.Extras.Community;

/// <summary>
/// Example community addon that demonstrates how you might plug
/// in custom JsonSerializerOptions when expanding environment JSON.
///
/// NOTE: The core JsonEnvOverrides implementation does not currently
/// accept JsonSerializerOptions; this file is intentionally illustrative.
/// You can adapt it to your own fork or project to experiment with
/// custom deserialization behavior.
/// </summary>
[JsonEnvOverridesExtra("CustomJsonSerializerExample", Description = "Sample extra showing how a custom serializer addon could be structured.")]
public sealed class CustomJsonSerializerExtra : IJsonEnvOverrideExtra
{
    public void Apply(IConfigurationBuilder builder)
    {
        // This is a no-op example. In your own project you could:
        // - Wrap the existing configuration builder,
        // - Add additional configuration sources that use custom JsonSerializerOptions,
        // - Or decorate the environment overrides with extra behavior.
        //
        // Because the main JsonEnvOverridesExtensions.cs file is intentionally
        // copy-paste friendly and minimal, we do not wire into it here.
        // Instead, treat this class as a starting point for your own experiments.
    }
}
