namespace Ciel.Birb;

public enum SegmentType
{
    Param,
    Path,
    Extra,
    Wildcard
}

public sealed class RouteSegment
{
    public SegmentType Type { get; set; }
    public string Value { get; set; } = string.Empty;


    public override string ToString()
    {
        return $"({Type} {Value})";
    }
}