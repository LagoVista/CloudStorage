using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class CosmosPathResolver
{
    // Matches a segment like: Areas[key=guides] or Pages[key=new]
    private static readonly Regex KeyedSegment =
        new Regex(@"^(?<name>[^\[]+)\[key=(?<key>.*)\]$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Converts a logical path that may include [key=...] segments into a Cosmos patch path with numeric indices.
    /// Example:
    ///   logical: /Areas[key=guides]/Pages[key=new]/CardTitle
    ///   cosmos:  /Areas/2/Pages/0/CardTitle
    /// </summary>
    public static string ResolveToCosmosPath(JToken targetDoc, string logicalPath)
    {
        if (targetDoc == null) throw new ArgumentNullException(nameof(targetDoc));
        if (string.IsNullOrWhiteSpace(logicalPath)) throw new ArgumentException("Path is required.", nameof(logicalPath));

        // Split on '/', ignore leading empty.
        var segments = logicalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // We build the resolved Cosmos path and simultaneously walk the target doc.
        var resolved = new List<string>(segments.Length);
        JToken current = targetDoc;

        foreach (var rawSeg in segments)
        {
            var seg = UnescapeJsonPointerSegment(rawSeg);

            // Handle numeric index segment already present (rare but supported)
            if (int.TryParse(seg, out var numericIndex))
            {
                current = StepArrayIndex(current, numericIndex, logicalPath);
                resolved.Add(numericIndex.ToString());
                continue;
            }

            // Handle keyed segment: Foo[key=bar]
            var m = KeyedSegment.Match(seg);
            if (m.Success)
            {
                var arrayName = m.Groups["name"].Value;
                var key = UnescapeKey(m.Groups["key"].Value);

                // Step into property "Foo"
                current = StepProperty(current, arrayName, logicalPath);

                // Must be array
                var arr = current as JArray;    
                if (current == null)
                    throw new InvalidOperationException($"Expected array at '{arrayName}' while resolving '{logicalPath}'.");

                // Find index where element.Key == key (case-insensitive)
                var idx = FindIndexByKey(arr, key);
                if (idx < 0)
                    throw new InvalidOperationException($"Could not find element with Key='{key}' in array '{arrayName}' while resolving '{logicalPath}'.");

                resolved.Add(arrayName);
                resolved.Add(idx.ToString());

                current = arr[idx]!;
                continue;
            }

            // Normal property segment
            current = StepProperty(current, seg, logicalPath);
            resolved.Add(seg);
        }

        return "/" + string.Join("/", resolved);
    }

    private static JToken StepProperty(JToken current, string propertyName, string logicalPath)
    {
        var obj = current as JObject;
        if (obj == null)
            throw new InvalidOperationException($"Expected object when stepping into '{propertyName}' while resolving '{logicalPath}'.");

        // Case-insensitive lookup
        var prop = obj.Property(propertyName, StringComparison.OrdinalIgnoreCase);
        if (prop == null)
            throw new InvalidOperationException($"Property '{propertyName}' not found while resolving '{logicalPath}'.");

        return prop.Value ?? JValue.CreateNull();
    }

    private static JToken StepArrayIndex(JToken current, int index, string logicalPath)
    {
        var arr = current as JArray;    
        if (arr == null)
            throw new InvalidOperationException($"Expected array when stepping into index [{index}] while resolving '{logicalPath}'.");

        if (index < 0 || index >= arr.Count)
            throw new IndexOutOfRangeException($"Index [{index}] out of range while resolving '{logicalPath}'.");

        return arr[index] ?? JValue.CreateNull();
    }

    private static int FindIndexByKey(JArray arr, string key)
    {
        for (var i = 0; i < arr.Count; i++)
        {
            var o = arr[i] as JObject;
            if (o == null) continue;

            var k = o.Property("Key", StringComparison.OrdinalIgnoreCase)?.Value?.ToString();
            if (!string.IsNullOrWhiteSpace(k) &&
                k.Equals(key, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    // If you used JSON Pointer escaping (~1, ~0) in paths:
    private static string UnescapeJsonPointerSegment(string seg)
        => seg.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);

    // If your keyed path escapes \] and \\ as in earlier helper:
    private static string UnescapeKey(string key)
        => key.Replace("\\]", "]", StringComparison.Ordinal).Replace("\\\\", "\\", StringComparison.Ordinal);
}
