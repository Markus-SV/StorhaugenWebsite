using System.Text.Json;

namespace StorhaugenEats.API.Helpers;

public static class JsonHelper
{
    /// <summary>
    /// Converts a JSON string to a List of strings
    /// </summary>
    public static List<string> JsonToList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Converts a List of strings to a JSON string
    /// </summary>
    public static string ListToJson(List<string>? list)
    {
        if (list == null || list.Count == 0)
        {
            return "[]";
        }

        return JsonSerializer.Serialize(list);
    }

    /// <summary>
    /// Safely gets a string from a nullable value, returns empty string if null
    /// </summary>
    public static string SafeString(string? value)
    {
        return value ?? string.Empty;
    }
}
