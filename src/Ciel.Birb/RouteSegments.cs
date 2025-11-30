using System.Text;

namespace Ciel.Birb;

public class RouteSegments
{
    public List<RouteSegment> Segments { get; init; } = new();

    public static RouteParams? PathMatch(string pattern, string path)
    {
        var segments = ParseForPath(pattern);
        return segments?.MatchForPath(path);
    }

    public static RouteParams? HostMatch(string pattern, string host)
    {
        var segments = ParseForHost(pattern);
        return segments?.MatchForHost(host);
    }


    public static RouteSegments ParseForPath(string pattern)
    {
        RouteSegments segments = new();
        var seenExtra = false;

        var i = 0;
        // Path segments
        while (i < pattern.Length && pattern[i] == '/')
        {
            // Consume '/'
            i++;

            // Ignore trailing slash
            if (i >= pattern.Length)
                break;

            if (seenExtra)
                throw new InvalidOperationException("EXTRA segment must be the last segment");

            var seg = new RouteSegment();

            if (pattern[i] == '{')
            {
                // {param} or {param...}
                i++; // skip '{'

                var startName = i;

                var extraIndex = pattern.IndexOf("...", startName, StringComparison.Ordinal);
                var braceIndex = pattern.IndexOf('}', startName);

                if (braceIndex == -1)
                    throw new InvalidOperationException("Unclosed '{' in route pattern");

                var isExtra = false;
                int nameEnd;

                if (extraIndex != -1 && extraIndex < braceIndex)
                {
                    // {name...}
                    isExtra = true;
                    nameEnd = extraIndex;
                }
                else
                {
                    // {name}
                    nameEnd = braceIndex;
                }

                var name = pattern[startName..nameEnd];

                seg.Value = name;

                if (isExtra)
                {
                    seg.Type = SegmentType.Extra;
                    if (seenExtra)
                        throw new InvalidOperationException("Multiple EXTRA segments not allowed");
                    seenExtra = true;
                }
                else
                {
                    seg.Type = SegmentType.Param;
                }

                // Move after '}' (and optional "...")
                i = braceIndex + 1;
            }
            else
            {
                // Literal path segment until next '/' or end
                var start = i;
                while (i < pattern.Length && pattern[i] != '/')
                    i++;

                var value = pattern[start..i];
                seg.Type = SegmentType.Path;
                seg.Value = value;
            }

            segments.Segments.Add(seg);
        }

        return segments;
    }

    public static RouteSegments ParseForHost(string pattern)
    {
        RouteSegments segments = new();
        var seenExtra = false;

        var i = 0;

        while (i < pattern.Length)
        {
            if (pattern[i] == '.')
                throw new InvalidOperationException("Empty domain segments are not allowed");

            var seg = new RouteSegment();

            if (pattern[i] == '{')
            {
                // {param} or {param...}
                i++; // skip '{'

                var startName = i;

                var extraIndex = pattern.IndexOf("...", startName, StringComparison.Ordinal);
                var braceIndex = pattern.IndexOf('}', startName);

                if (braceIndex == -1)
                    throw new InvalidOperationException("Unclosed '{' in domain pattern");

                var isExtra = false;
                int nameEnd;

                if (extraIndex != -1 && extraIndex < braceIndex)
                {
                    // {name...}
                    isExtra = true;
                    nameEnd = extraIndex;
                }
                else
                {
                    // {name}
                    nameEnd = braceIndex;
                }

                if (nameEnd == startName)
                    throw new InvalidOperationException("Empty parameter name in domain pattern");

                var name = pattern[startName..nameEnd];
                seg.Value = name;

                if (isExtra)
                {
                    seg.Type = SegmentType.Extra;
                    if (seenExtra)
                        throw new InvalidOperationException("Multiple EXTRA segments not allowed in domain pattern");
                    seenExtra = true;
                }
                else
                {
                    seg.Type = SegmentType.Param;
                }

                // Move after '}'
                i = braceIndex + 1;
            }
            else if (pattern[i] == '*')
            {
                // Wildcard segment: matches any single label, not captured
                seg.Type = SegmentType.Wildcard;
                seg.Value = "*";
                i++; // consume '*'
            }
            else
            {
                // Literal domain label until next '.' or end
                var start = i;
                while (i < pattern.Length && pattern[i] != '.')
                    i++;

                var value = pattern[start..i];
                if (value.Length == 0)
                    throw new InvalidOperationException("Empty domain segment");

                seg.Type = SegmentType.Path;
                seg.Value = value;
            }

            segments.Segments.Add(seg);

            // Consume '.' between labels, if any
            if (i < pattern.Length)
            {
                if (pattern[i] != '.')
                    throw new InvalidOperationException(
                        $"Unexpected character '{pattern[i]}' in domain pattern, expected '.'");

                i++; // skip '.'
            }
        }

        return segments;
    }

    public RouteParams? MatchForPath(string path)
    {
        var parts = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var index = 0;
        var @params = new RouteParams();

        foreach (var seg in Segments)
        {
            // Allow Extra to match even if nothing is left
            if (seg.Type != SegmentType.Extra && index >= parts.Length)
                return null;

            switch (seg.Type)
            {
                case SegmentType.Param:
                    @params.Put(seg.Value, parts[index]);
                    index++;
                    break;

                case SegmentType.Path:
                    if (!string.Equals(seg.Value, parts[index], StringComparison.Ordinal))
                        return null;
                    index++;
                    break;

                case SegmentType.Extra:
                    // capture rest, including empty remainder
                    if (index >= parts.Length)
                    {
                        @params.Put(seg.Value, "");
                        return @params;
                    }

                    var sb = new StringBuilder();
                    for (var first = true; index < parts.Length; index++)
                    {
                        if (!first) sb.Append('/');
                        sb.Append(parts[index]);
                        first = false;
                    }

                    @params.Put(seg.Value, sb.ToString());
                    return @params;

                default:
                    throw new InvalidOperationException("Unreachable segment type");
            }
        }

        if (index < parts.Length)
            return null;

        return @params;
    }


    public RouteParams? MatchForHost(string host)
    {
        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        var index = 0;
        var @params = new RouteParams();

        for (var s = 0; s < Segments.Count; s++)
        {
            var seg = Segments[s];

            // SPECIAL CASE: EXTRA can be in the middle for domains
            if (seg.Type == SegmentType.Extra)
            {
                // Segments after this EXTRA
                var segsAfter = Segments.Count - s - 1;

                // We must have at least segsAfter parts left to match the suffix
                if (parts.Length - index < segsAfter)
                    return null;

                // All parts from index to extraEnd belong to the EXTRA param
                var extraEnd = parts.Length - segsAfter;
                var extraValue = string.Join('.', parts[index..extraEnd]);
                @params.Put(seg.Value, extraValue);

                // Now match the tail segments against the tail parts
                var p = extraEnd;
                for (var t = s + 1; t < Segments.Count; t++)
                {
                    if (p >= parts.Length)
                        return null;

                    var tailSeg = Segments[t];

                    switch (tailSeg.Type)
                    {
                        case SegmentType.Param:
                            @params.Put(tailSeg.Value, parts[p]);
                            p++;
                            break;

                        case SegmentType.Path:
                            if (!string.Equals(tailSeg.Value, parts[p], StringComparison.OrdinalIgnoreCase))
                                return null;
                            p++;
                            break;

                        case SegmentType.Wildcard:
                            // Matches exactly one label, no capture
                            p++;
                            break;

                        case SegmentType.Extra:
                            // Should be prevented by parser
                            throw new InvalidOperationException(
                                "Multiple EXTRA segments not allowed in domain pattern");

                        default:
                            throw new InvalidOperationException("Unreachable segment type");
                    }
                }

                // All remaining labels must have been consumed
                if (p != parts.Length)
                    return null;

                return @params;
            }

            // Normal (non-EXTRA) segment handling
            if (index >= parts.Length)
                return null;

            switch (seg.Type)
            {
                case SegmentType.Param:
                    @params.Put(seg.Value, parts[index]);
                    index++;
                    break;

                case SegmentType.Path:
                    if (!string.Equals(seg.Value, parts[index], StringComparison.OrdinalIgnoreCase))
                        return null;
                    index++;
                    break;

                case SegmentType.Wildcard:
                    // Match any single label, no capture
                    index++;
                    break;

                default:
                    throw new InvalidOperationException("Unreachable segment type");
            }
        }

        // No leftover labels
        if (index < parts.Length)
            return null;

        return @params;
    }
}