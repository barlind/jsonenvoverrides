using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace JsonEnvOverrides.Tests;

public class JsonEnvOverridesExtensionsTests
{
    private const string RootPrefix = "MyApp";

    private static string ReadTestJson(string relativePath)
    {
        var baseDir = AppContext.BaseDirectory;
        var fullPath = Path.Combine(baseDir, "TestData", relativePath);
        return File.ReadAllText(fullPath);
    }

    private static IConfiguration BuildConfigWithEnv(IDictionary<string, string?> envVars)
    {
        // Clear any previous test variables under this prefix
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (key != null && key.StartsWith(RootPrefix + "__", StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        foreach (var kvp in envVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        builder.AddJsonEnvOverrides(RootPrefix);
        return builder.Build();
    }

    [Fact]
    public void Expands_Json_Array_Into_Indexed_Keys()
    {
        var config = BuildConfigWithEnv(new Dictionary<string, string?>
        {
            ["MyApp__Teams"] = ReadTestJson("array-teams.json")
        });

        Assert.Equal("a", config["MyApp:Teams:0"]);
        Assert.Equal("b", config["MyApp:Teams:1"]);
        Assert.Equal("c", config["MyApp:Teams:2"]);
    }

    [Fact]
    public void Expands_Nested_Object_And_Array()
    {
        var config = BuildConfigWithEnv(new Dictionary<string, string?>
        {
            ["MyApp__Override"] = ReadTestJson("nested-object-list.json")
        });

        Assert.Equal("x", config["MyApp:Override:Nested:List:0:Name"]);
        Assert.Equal("1", config["MyApp:Override:Nested:List:0:Value"]);
        Assert.Equal("y", config["MyApp:Override:Nested:List:1:Name"]);
        Assert.Equal("2", config["MyApp:Override:Nested:List:1:Value"]);
    }

    [Fact]
    public void Ignores_NonJson_Values()
    {
        var config = BuildConfigWithEnv(new Dictionary<string, string?>
        {
            ["MyApp__Plain"] = "not json"
        });

        // Extension should not treat non-JSON specially; base provider exposes it
        Assert.Equal("not json", config["MyApp:Plain"]);
    }

    [Fact]
    public void Throws_On_Invalid_Json()
    {
        // Ensure a clean slate for this prefix
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (key != null && key.StartsWith(RootPrefix + "__", StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        Environment.SetEnvironmentVariable("MyApp__Broken", ReadTestJson("invalid.json"));

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            builder.AddJsonEnvOverrides(RootPrefix, continueOnError: false);
        });

        Assert.Contains("MyApp__Broken", ex.Message);
    }

    [Fact]
    public void Continues_When_Invalid_Json_If_ContinueOnError_True()
    {
        // Clean prefix env vars
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (key != null && key.StartsWith(RootPrefix + "__", StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        Environment.SetEnvironmentVariable("MyApp__Broken", ReadTestJson("invalid.json"));
        Environment.SetEnvironmentVariable("MyApp__Teams", ReadTestJson("array-teams.json"));

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        builder.AddJsonEnvOverrides(RootPrefix, continueOnError: true);

        var config = builder.Build();

        // Broken should not be expanded, but valid Teams should be
        Assert.Null(config["MyApp:Broken:0"]);
        Assert.Equal("a", config["MyApp:Teams:0"]);
        Assert.Equal("b", config["MyApp:Teams:1"]);
        Assert.Equal("c", config["MyApp:Teams:2"]);
    }

    private class MyAppSettings
    {
        public List<string> Teams { get; set; } = new();
        public NestedSettings Nested { get; set; } = new();
        public SomeFeatureSettings SomeFeature { get; set; } = new();
    }

    private class NestedSettings
    {
        public List<ListItem> List { get; set; } = new();
    }

    private class ListItem
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; } = 0;
    }

    private class SomeFeatureSettings
    {
        public bool Enabled { get; set; } = false;
    }

    [Fact]
    public void Integration_Appsettings_And_Env_Json_Override_Binds_As_Expected()
    {
        // Clean any previous env for this prefix
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (key != null && key.StartsWith(RootPrefix + "__", StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        // Override as shown in README
        Environment.SetEnvironmentVariable("MyApp__Teams", ReadTestJson("array-teams.json"));
        Environment.SetEnvironmentVariable("MyApp__Nested__List", ReadTestJson("nested-object-list.json"));
        Environment.SetEnvironmentVariable("MyApp__SomeFeature", "{ \"Enabled\": false }");

        var baseDir = AppContext.BaseDirectory;
        var appsettingsPath = Path.Combine(baseDir, "TestData", "appsettings.sample.json");

        var builder = new ConfigurationBuilder()
            .AddJsonFile(appsettingsPath, optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddJsonEnvOverrides(RootPrefix);

        var config = builder.Build();

        // Teams list should come entirely from env JSON
        Assert.Equal("a", config["MyApp:Teams:0"]);
        Assert.Equal("b", config["MyApp:Teams:1"]);
        Assert.Equal("c", config["MyApp:Teams:2"]);

        // Nested list should be the original from appsettings,
        // because we overrode MyApp__Nested__List as a separate root
        Assert.Equal("a", config["MyApp:Nested:List:0:Name"]);
        Assert.Equal("1", config["MyApp:Nested:List:0:Value"]);
        Assert.Equal("b", config["MyApp:Nested:List:1:Name"]);
        Assert.Equal("2", config["MyApp:Nested:List:1:Value"]);

        // SomeFeature.Enabled overridden to false via JSON object (bool casing may vary)
        Assert.Equal("false", config["MyApp:SomeFeature:Enabled"], ignoreCase: true);

        // Manually map a small subset into strongly-typed settings and verify values
        var bound = new MyAppSettings
        {
            Teams = new List<string>
            {
                config["MyApp:Teams:0"]!,
                config["MyApp:Teams:1"]!,
                config["MyApp:Teams:2"]!,
            },
            Nested = new NestedSettings
            {
                List = new List<ListItem>
                {
                    new ListItem
                    {
                        Name = config["MyApp:Nested:List:0:Name"]!,
                        Value = int.Parse(config["MyApp:Nested:List:0:Value"]!),
                    },
                    new ListItem
                    {
                        Name = config["MyApp:Nested:List:1:Name"]!,
                        Value = int.Parse(config["MyApp:Nested:List:1:Value"]!),
                    },
                }
            },
            SomeFeature = new SomeFeatureSettings
            {
                Enabled = bool.Parse(config["MyApp:SomeFeature:Enabled"] ?? "false")
            }
        };

        Assert.Equal(new[] { "a", "b", "c" }, bound.Teams);
        Assert.Equal("a", bound.Nested.List[0].Name);
        Assert.Equal(1, bound.Nested.List[0].Value);
        Assert.Equal("b", bound.Nested.List[1].Name);
        Assert.Equal(2, bound.Nested.List[1].Value);
        Assert.False(bound.SomeFeature.Enabled);
    }

    [Fact]
    public void Negative_Binding_When_Section_Missing_Yields_Defaults()
    {
        var options = new MyAppSettings();

        Assert.Empty(options.Teams);
        Assert.Empty(options.Nested.List);
        Assert.False(options.SomeFeature.Enabled);
    }
}
