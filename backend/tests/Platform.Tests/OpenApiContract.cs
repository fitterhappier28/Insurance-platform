using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Shouldly;
using YamlDotNet.Serialization;

namespace Platform.Tests;

public static class OpenApiContract
{
    public static void AssertOperationResponse(string contractPath, string operationId, string status, string body)
    {
        var document = Load(contractPath);
        var schema = FindResponseSchema(document, operationId, status) ?? throw new InvalidOperationException(
            $"Operation '{operationId}' has no application/problem+json schema for status {status}.");
        AssertSchema(schema, body);
    }

    public static void AssertProblemSchema(string body)
    {
        var common = Load(Path.Combine(RepoRoot(), "specs", "contracts", "openapi", "common.v1.yaml"));
        var schema = common["components"]!["schemas"]!["Problem"]!;
        AssertSchema(schema, body);
    }

    public static void AssertServedDocument(string served, string contractPath)
    {
        var expected = File.ReadAllText(contractPath).Replace("\r\n", "\n", StringComparison.Ordinal);
        served.Replace("\r\n", "\n", StringComparison.Ordinal).ShouldBe(expected);
    }

    private static void AssertSchema(JsonNode schema, string body)
    {
        var compiled = JsonSchema.FromText(schema.ToJsonString());
        using var instance = JsonDocument.Parse(body);
        var evaluation = compiled.Evaluate(instance.RootElement);
        evaluation.IsValid.ShouldBeTrue(evaluation.ToString());
    }

    private static JsonNode? FindResponseSchema(JsonObject document, string operationId, string status)
    {
        foreach (var path in document["paths"]!.AsObject())
        {
            foreach (var operation in path.Value!.AsObject())
            {
                if (operation.Value?["operationId"]?.GetValue<string>() != operationId)
                {
                    continue;
                }

                var responses = operation.Value["responses"]?.AsObject();
                if (responses is null)
                {
                    continue;
                }

                JsonNode? response = responses[status];
                var owner = document;
                if (response?["$ref"] is JsonNode responseReference)
                {
                    (owner, response) = Resolve(owner, responseReference.GetValue<string>());
                }

                var schema = response?["content"]?["application/problem+json"]?["schema"];
                if (schema?["$ref"] is JsonNode schemaReference)
                {
                    return Resolve(owner, schemaReference.GetValue<string>()).Node;
                }

                return schema;
            }
        }

        return null;
    }

    private static (JsonObject Owner, JsonNode Node) Resolve(JsonObject document, string reference)
    {
        if (reference.StartsWith("common.v1.yaml#/", StringComparison.Ordinal))
        {
            var common = Load(Path.Combine(RepoRoot(), "specs", "contracts", "openapi", "common.v1.yaml"));
            return (common, Walk(common, reference["common.v1.yaml#/".Length..]));
        }

        return (document, Walk(document, reference["#/".Length..]));
    }

    private static JsonNode Walk(JsonObject document, string pointer)
    {
        JsonNode current = document;
        foreach (var segment in pointer.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current[segment]!;
        }

        return current;
    }

    private static JsonObject Load(string path)
    {
        var yaml = new DeserializerBuilder().Build().Deserialize(File.ReadAllText(path));
        return (JsonObject)ToNode(yaml)!;
    }

    private static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        string text when text is "true" or "false" => JsonValue.Create(bool.Parse(text)),
        string text => JsonValue.Create(text),
        bool flag => JsonValue.Create(flag),
        int number => JsonValue.Create(number),
        long number => JsonValue.Create(number),
        double number => JsonValue.Create(number),
        Dictionary<object, object> map => Map(map),
        IDictionary<object, object> objectMap => Map(objectMap.ToDictionary(pair => pair.Key, pair => pair.Value)),
        IDictionary<string, object> stringMap => Map(stringMap.ToDictionary(pair => (object)pair.Key, pair => pair.Value)),
        System.Collections.IDictionary dictionary => Map(dictionary.Keys.Cast<object>().ToDictionary(key => key, key => dictionary[key]!)),
        System.Collections.IEnumerable list when value is not string => List(list.Cast<object>()),
        _ => JsonValue.Create(value.ToString())
    };

    private static JsonObject Map(Dictionary<object, object> map)
    {
        var result = new JsonObject();
        foreach (var (key, value) in map)
        {
            result[key.ToString()!] = ToNode(value);
        }

        return result;
    }

    private static JsonArray List(IEnumerable<object> list)
    {
        var result = new JsonArray();
        foreach (var item in list)
        {
            result.Add(ToNode(item));
        }

        return result;
    }

    public static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "specs")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
