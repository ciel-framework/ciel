using System.Text;

namespace Ciel.Birb;

public class RouteParams : Dictionary<string, string>
{
    public void Put(string key, string value)
    {
        this[key] = value;
    }
}

public sealed class RoutePattern
{
    private Method Method { get; set; }
    private RouteSegments? HostSegments { get; set; }
    private RouteSegments PathSegments { get; set; }


    // Equivalent of: static RoutePattern parse(Str str)
    // "GET foo.example.com/users/{id}"
    public static RoutePattern Parse(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        pattern = pattern.TrimStart();

        // First token = HTTP method
        var spaceIndex = pattern.IndexOf(' ');
        if (spaceIndex < 0)
            throw new InvalidOperationException("Invalid pattern: missing method");

        var methodStr = pattern[..spaceIndex];
        if (!Enum.TryParse<Method>(methodStr, true, out var method))
            throw new InvalidOperationException($"Invalid pattern method: {methodStr}");

        var rest = pattern[(spaceIndex + 1)..];
        return Parse(method, rest);
    }

    // Equivalent of: static RoutePattern parse(Method method, Str str)
    public static RoutePattern Parse(Method method, string pattern)
    {
        var p = new RoutePattern
        {
            Method = method
        };

        if (pattern is null)
            throw new ArgumentNullException(nameof(pattern));

        pattern = pattern.TrimStart();

        var i = 0;

        if (i < pattern.Length && pattern[i] != '/')
        {
            var slashIndex = pattern.IndexOf('/', i);
            if (slashIndex < 0)
            {
                p.HostSegments = RouteSegments.ParseForHost(pattern[i..].Trim());
                return p;
            }

            p.HostSegments = RouteSegments.ParseForHost(pattern[i..slashIndex].Trim());
            i = slashIndex;
        }

        p.PathSegments = RouteSegments.ParseForPath(pattern[i..].Trim());

        return p;
    }

    public MatchResult? Match(Method method, Uri url)
    {
        MatchResult result = new();

        if (Method != method)
            return null;

        if (HostSegments is not null)
        {
            var hostParms = HostSegments.MatchForHost(url.Host);
            if (hostParms == null)
                return null;
            result.HostParams = hostParms;
        }

        var pathParams = PathSegments.MatchForPath(url.AbsolutePath);
        if (pathParams == null)
            return null;
        result.PathParams = pathParams;

        return result;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("(pattern ");
        sb.Append(Method);
        sb.Append(' ');

        if (HostSegments is not null)
        {
            sb.Append('[');
            for (var i = 0; i < HostSegments.Segments.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(HostSegments.Segments[i]);
            }

            sb.Append("]");
            sb.Append(' ');
        }

        sb.Append('[');
        for (var i = 0; i < PathSegments.Segments.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(PathSegments.Segments[i]);
        }

        sb.Append("]");
        sb.Append(")");
        return sb.ToString();
    }

    public class MatchResult
    {
        public RouteParams HostParams { get; set; }
        public RouteParams PathParams { get; set; }
    }
}