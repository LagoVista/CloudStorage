using LagoVista.Core.Models.UIMetaData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public static class ListRequestCacheSig
{
    // Returns something like: "req:9f3a21c0" (8-char SHA256 prefix)
    public static string ReqSig(this ListRequest r)
    {
        if (r == null) return "req:0";

        var parts = new List<string>(32);

        // Paging / keyset
        Add(parts, "ps", r.PageSize != 0 ? r.PageSize.ToString(CultureInfo.InvariantCulture) : null);
        Add(parts, "pi", r.PageIndex != 0 ? r.PageIndex.ToString(CultureInfo.InvariantCulture) : null);
        Add(parts, "npk", Norm(r.NextPartitionKey));
        Add(parts, "nrk", Norm(r.NextRowKey));

        // Filters
        Add(parts, "sd", Norm(r.StartDate));
        Add(parts, "ed", Norm(r.EndDate));
        Add(parts, "cat", Norm(r.CategoryKey));

        // Grouping
        Add(parts, "gb", Norm(r.GroupBy));
        Add(parts, "gbt", Norm(r.GroupByType));
        Add(parts, "gbs", r.GroupBySize != 0 ? r.GroupBySize.ToString(CultureInfo.InvariantCulture) : null);

        // Time bucket
        Add(parts, "tb", Norm(r.TimeBucket));
        Add(parts, "tbs", r.TimeBucketSize != 0 ? r.TimeBucketSize.ToString(CultureInfo.InvariantCulture) : null);

        // Flags
        Add(parts, "drafts", r.ShowDrafts ? "1" : null);
        Add(parts, "deleted", r.ShowDeleted ? "1" : null);

        // Sort (only one should typically be set, but support both deterministically)
        Add(parts, "ob", r.OrderBy?.ToString());
        Add(parts, "obd", r.OrderByDesc?.ToString());

        // Usually exclude Url (often not data-shaping). If it is, uncomment:
        // Add(parts, "url", Norm(r.Url));

        var canonical = string.Join(";", parts);
        var h = Sha256Hex(canonical);

        // 8 chars is usually enough to avoid collisions in practice; bump to 12 if you prefer.
        return $"req:{h.Substring(0, 8)}";
    }

    private static void Add(List<string> parts, string k, string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return;
        parts.Add($"{k}={v}");
    }

    private static string Norm(string s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Sha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
        var sb = new StringBuilder(bytes.Length * 2);
        for (var i = 0; i < bytes.Length; i++)
            sb.Append(bytes[i].ToString("x2"));
        var req = sb.ToString();
        return $"req:{req.Substring(0, 12)}"; // 48 bits
    }
}