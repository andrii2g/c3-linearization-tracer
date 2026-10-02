using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using C3Tracer.Core.Model;
using C3Tracer.Core.Validation;

namespace C3Tracer.Cli.Input;

public static class HierarchyJsonReader
{
    public const int MaxBytes = 1024 * 1024;

    public static async Task<InputResult> ReadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var bytes = new byte[MaxBytes + 1];
        int count = 0;
        while (count < bytes.Length)
        {
            int read = await stream.ReadAsync(bytes.AsMemory(count));
            if (read == 0) break;
            count += read;
        }
        return Parse(bytes.AsMemory(0, count));
    }

    public static InputResult Parse(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length > MaxBytes) return Error("LimitExceeded", "Input exceeds 1 MiB.");
        try
        {
            // JsonDocument can defer decoding string values; reject malformed UTF-8 up front.
            string json = new UTF8Encoding(false, true).GetString(bytes.Span);
            var depthReader = new Utf8JsonReader(bytes.Span, new JsonReaderOptions { MaxDepth = int.MaxValue });
            while (depthReader.Read())
                if (depthReader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject && depthReader.CurrentDepth >= 16)
                    return Error("LimitExceeded", "JSON nesting exceeds 16.");
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            var errors = new List<Diagnostic>();
            var root = document.RootElement;
            if (!Object(root, "", ["target", "classes"], ["classes"], errors))
                return new(null, GraphValidator.Order(errors));
            string? target = null;
            if (root.TryGetProperty("target", out var t))
            {
                if (t.ValueKind != JsonValueKind.String) errors.Add(new("InvalidShape", "target must be a string.", "/target"));
                else
                {
                    target = t.GetString();
                    if (!GraphValidator.IsName(target)) errors.Add(new("InvalidName", "Invalid target name.", "/target"));
                }
            }
            var classes = ImmutableArray.CreateBuilder<ClassDefinition>();
            if (root.TryGetProperty("classes", out var array))
            {
                if (array.ValueKind != JsonValueKind.Array) errors.Add(new("InvalidShape", "classes must be an array.", "/classes"));
                else if (array.GetArrayLength() > GraphValidator.MaxClasses) errors.Add(new("LimitExceeded", "At most 256 classes are allowed.", "/classes"));
                else
                {
                    int i = 0;
                    foreach (var element in array.EnumerateArray())
                    {
                        string p = $"/classes/{i++}";
                        if (!Object(element, p, ["name", "bases"], ["name", "bases"], errors)) continue;
                        string? name = null;
                        if (element.TryGetProperty("name", out var n))
                        {
                            if (n.ValueKind != JsonValueKind.String) errors.Add(new("InvalidShape", "name must be a string.", p + "/name"));
                            else name = n.GetString();
                        }
                        var bases = ImmutableArray.CreateBuilder<string>();
                        if (element.TryGetProperty("bases", out var b))
                        {
                            if (b.ValueKind != JsonValueKind.Array) errors.Add(new("InvalidShape", "bases must be an array.", p + "/bases"));
                            else if (b.GetArrayLength() > GraphValidator.MaxBases) errors.Add(new("LimitExceeded", "At most 32 direct bases are allowed.", p + "/bases"));
                            else
                            {
                                int j = 0;
                                foreach (var value in b.EnumerateArray())
                                {
                                    if (value.ValueKind != JsonValueKind.String) errors.Add(new("InvalidShape", "A base must be a string.", p + $"/bases/{j}"));
                                    else if (value.GetString() is string valueName) bases.Add(valueName);
                                    j++;
                                }
                            }
                        }
                        if (name is not null) classes.Add(new(name, bases.ToImmutable()));
                    }
                }
            }
            return errors.Count > 0 ? new(null, GraphValidator.Order(errors), target) : new(new(target, new(classes.ToImmutable())), [], target);
        }
        catch (DecoderFallbackException) { return Error("InvalidJson", "Input is not valid UTF-8."); }
        catch (JsonException)
        {
            return Error("InvalidJson", "Invalid JSON syntax.");
        }
    }

    private static bool Object(JsonElement value, string path, string[] allowed, string[] required, List<Diagnostic> errors)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new("InvalidShape", "Expected an object.", path));
            return false;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            string p = path + "/" + property.Name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
            if (!seen.Add(property.Name)) errors.Add(new("DuplicateProperty", $"Repeated property {property.Name}.", p));
            if (!allowed.Contains(property.Name, StringComparer.Ordinal)) errors.Add(new("UnknownProperty", $"Unknown property {property.Name}.", p));
        }
        foreach (string name in required)
            if (!seen.Contains(name)) errors.Add(new("InvalidShape", $"Missing required property {name}.", path + "/" + name));
        return true;
    }

    private static InputResult Error(string code, string message) => new(null, [new(code, message)]);
}
