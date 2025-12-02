using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace JsonEnvOverrides
{
    /// <summary>
    /// Adds support for overriding hierarchical configuration (including arrays/objects)
    /// using JSON stored in environment variables.
    ///
    /// Example:
    ///   appsettings.json:
    ///     "MyApp": {
    ///       "Teams": [ "team1", "team2" ]
    ///     }
    ///
    ///   Environment variable:
    ///     MyApp__Teams = ["prod1","prod2","prod3"]
    ///
    ///   Resulting configuration keys:
    ///     MyApp:Teams:0 = "prod1"
    ///     MyApp:Teams:1 = "prod2"
    ///     MyApp:Teams:2 = "prod3"
    ///
    /// Only environment variables whose names start with the given rootPrefix and whose
    /// values look like JSON (starting with '{' or '[') are processed.
    /// Everything else is left to the normal EnvironmentVariablesConfigurationProvider.
    /// </summary>
    public static class JsonEnvOverridesExtensions
    {
        /// <summary>
        /// Scans environment variables for keys starting with <paramref name="rootPrefix"/> + "__",
        /// and for any whose value looks like JSON (starts with '{' or '['), parses that JSON and
        /// expands it into hierarchical configuration keys via an in-memory provider added last.
        ///
        /// This lets you paste JSON arrays/objects directly into App Service / env settings and have
        /// them bind correctly to List&lt;T&gt; and complex types.
        /// </summary>
        /// <param name="builder">The configuration builder.</param>
        /// <param name="rootPrefix">
        /// The logical root for the section, e.g. "MyApp" or "MyApp".
        /// Environment variables should then use names like "MyApp__Teams", "MyApp__Nested__List", etc.
        /// </param>
        public static IConfigurationBuilder AddJsonEnvOverrides(
            this IConfigurationBuilder builder,
            string rootPrefix,
            bool continueOnError = false)
        {
            if (string.IsNullOrWhiteSpace(rootPrefix))
                throw new ArgumentException("rootPrefix must be non-empty", nameof(rootPrefix));

            var env = Environment.GetEnvironmentVariables();
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            // We'll look for env vars starting with e.g. "MyApp__"
            var prefix = NormalizePrefix(rootPrefix);

            foreach (DictionaryEntry entry in env)
            {
                var rawKey = entry.Key?.ToString();
                var rawValue = entry.Value?.ToString();

                if (!ShouldProcessEnvVar(rawKey, rawValue, prefix))
                    continue;

                var logicalKey = rawKey!.Replace("__", ":", StringComparison.Ordinal);

                TryExpandJsonValue(rawKey, rawValue!, logicalKey, dict, continueOnError);
            }

            if (dict.Count > 0)
            {
                // Add as highest-priority provider so these overrides win.
                builder.AddInMemoryCollection(dict);
            }

            return builder;
        }

        private static string NormalizePrefix(string rootPrefix)
        {
            return rootPrefix.EndsWith("__", StringComparison.Ordinal)
                ? rootPrefix
                : rootPrefix + "__";
        }

        private static bool ShouldProcessEnvVar(string? rawKey, string? rawValue, string prefix)
        {
            if (string.IsNullOrEmpty(rawKey))
                return false;

            if (!rawKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            var trimmed = rawValue.TrimStart();
            if (trimmed.Length == 0)
                return false;

            // Only treat env vars whose values look like JSON specially.
            var first = trimmed[0];
            return first == '[' || first == '{';
        }

        private static void TryExpandJsonValue(
            string rawKey,
            string rawValue,
            string logicalKey,
            IDictionary<string, string?> dict,
            bool continueOnError)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawValue);
                ExpandJsonElement(logicalKey, doc.RootElement, dict);
            }
            catch (JsonException ex)
            {
                if (!continueOnError)
                {
                    throw new InvalidOperationException(
                        $"Invalid JSON in environment variable '{rawKey}'.", ex);
                }

                // Best-effort mode: skip this env var and continue processing others.
            }
        }

        private static void ExpandJsonElement(
            string path,
            JsonElement element,
            IDictionary<string, string?> dict)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        var childPath = $"{path}:{prop.Name}";
                        ExpandJsonElement(childPath, prop.Value, dict);
                    }
                    break;

                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var childPath = $"{path}:{index}";
                        ExpandJsonElement(childPath, item, dict);
                        index++;
                    }
                    break;

                case JsonValueKind.String:
                    dict[path] = element.GetString();
                    break;

                case JsonValueKind.Number:
                    // Use raw text to preserve formatting (decimals, etc.).
                    dict[path] = element.GetRawText();
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    dict[path] = element.GetBoolean().ToString();
                    break;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    dict[path] = null;
                    break;

                default:
                    // Fallback: store raw representation.
                    dict[path] = element.GetRawText();
                    break;
            }
        }
    }
}
