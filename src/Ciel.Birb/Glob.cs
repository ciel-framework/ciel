using System.Text.RegularExpressions;

namespace Ciel.Birb;

public static class Glob
{
    public static bool Match(string pattern, string value)
    {
        if (string.IsNullOrEmpty(pattern) || pattern == "*")
            return true;

        // Escape regex chars, then replace \* with .*
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }
}