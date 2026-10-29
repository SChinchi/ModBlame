using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ModBlame;

internal class Utils
{
    internal static bool MatchPattern(string text, List<Regex> patterns)
    {
        return patterns.Any(p => p.IsMatch(text));
    }

    internal static string[] SplitText(string text, char separator = ',')
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        List<string> result = [];
        string[] parts = text.Split(separator);
        foreach (string part in parts)
        {
            if (string.IsNullOrWhiteSpace(part) == false)
            {
                result.Add(part.Trim());
            }
        }
        return [.. result];
    }
}
