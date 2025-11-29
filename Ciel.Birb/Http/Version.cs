namespace Ciel.Birb;

public readonly struct Version(Protocol protocol, ushort major, ushort minor)
{
    public Protocol Protocol { get; } = protocol;
    public ushort Major { get; } = major;
    public ushort Minor { get; } = minor;

    public static readonly Version Http10 = new(Protocol.Http, 1, 0);
    public static readonly Version Http11 = new(Protocol.Http, 1, 1);

    public static Version From(string str)
    {
        // Minimal support for HTTP/1.0 and 1.1
        if (str.Equals("HTTP/1.0", StringComparison.OrdinalIgnoreCase)) return Http10;
        if (str.Equals("HTTP/1.1", StringComparison.OrdinalIgnoreCase)) return Http11;
        throw new FormatException($"Unsupported HTTP version: {str}");
    }

    public override string ToString()
    {
        return $"{Protocol.ToString().ToUpperInvariant()}/{Major}.{Minor}";
    }
}