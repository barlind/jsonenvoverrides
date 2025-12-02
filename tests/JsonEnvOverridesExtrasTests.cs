using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using JsonEnvOverrides;
using Xunit;
using JsonEnvOverrides.Extras;

namespace JsonEnvOverrides.Tests;

public class JsonEnvOverridesExtrasTests
{
    [Fact]
    public void AddJsonEnvOverridesExtras_Discovers_And_Applies_Extras()
    {
        var builder = new ConfigurationBuilder();

        // Use this assembly so the discovery can find our dummy extra below.
        builder.AddJsonEnvOverridesExtras(Assembly.GetExecutingAssembly());

        var config = builder.Build();

        Assert.Equal("from-extra", config["Extras:dummy"]);
    }

    [Fact]
    public void AddJsonEnvOverridesExtras_Uses_Default_Assembly_When_None_Provided()
    {
        var builder = new ConfigurationBuilder();

        // Exercise the overload that relies on default assembly selection logic.
        builder.AddJsonEnvOverridesExtras();

        var config = builder.Build();

        Assert.Equal("from-extra", config["Extras:dummy"]);
    }
}

[JsonEnvOverridesExtra("DummyExtraForTests", "Adds a simple config value for testing.")]
public sealed class DummyTestExtra : IJsonEnvOverrideExtra
{
    #pragma warning disable IDE0060 // Make 'Apply' static
    public void Apply(IConfigurationBuilder builder)
    {
        var values = new Dictionary<string, string?>
        {
            ["Extras:dummy"] = "from-extra"
        };

        builder.AddInMemoryCollection(values);
    }
    #pragma warning restore IDE0060
}
