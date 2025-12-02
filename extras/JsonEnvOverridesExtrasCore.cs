using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace JsonEnvOverrides.Extras;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class JsonEnvOverridesExtraAttribute : Attribute
{
    public string Name { get; }
    public string? Description { get; }

    public JsonEnvOverridesExtraAttribute(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}

public interface IJsonEnvOverrideExtra
{
    void Apply(IConfigurationBuilder builder);
}

public static class JsonEnvOverridesExtrasDiscovery
{
    public static IConfigurationBuilder AddJsonEnvOverridesExtras(
        this IConfigurationBuilder builder,
        params Assembly[]? assemblies)
    {
        var toScan = (assemblies is { Length: > 0 }
            ? assemblies
            : new[] { Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly() }
                .Where(a => a is not null)
                .Cast<Assembly>()
                .ToArray());

        foreach (var assembly in toScan)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract)
                    continue;

                if (!typeof(IJsonEnvOverrideExtra).IsAssignableFrom(type))
                    continue;

                if (type.GetCustomAttribute<JsonEnvOverridesExtraAttribute>() is null)
                    continue;

                if (Activator.CreateInstance(type) is IJsonEnvOverrideExtra extra)
                {
                    extra.Apply(builder);
                }
            }
        }

        return builder;
    }
}
