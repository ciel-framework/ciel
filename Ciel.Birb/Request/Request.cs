namespace Ciel.Birb;

public class Request
{
    public Method Method { get; private init; } = Method.Get;
    public string RawPath { get; private init; } = "/";

    public string Path
    {
        get
        {
            var path = RawPath;
            var q = path.IndexOfAny(['?', '#']);
            if (q >= 0)
                path = path[..q];
            return Uri.UnescapeDataString(path).Replace('\\', '/');
        }
    }

    public Version Version { get; private init; } = Version.Http11;
    public Headers Headers { get; private init; } = new();
    public Body Body { get; private init; } = null!;

    private static Tuple<Method, string, Version> ParseStartLine(string str)
    {
        var parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
            throw new FormatException($"Invalid request line: {str}");

        if (!Enum.TryParse(parts[0], true, out Method method))
            throw new FormatException($"Unknown method: {parts[0]}");

        var path = parts[1];
        var version = Version.From(parts[2]);

        return Tuple.Create(method, path, version);
    }

    public static async Task<Request> ReadHeaderAsync(Stream stream)
    {
        var startLine = (await stream.ReadLineAsync() ?? string.Empty).TrimEnd();
        if (string.IsNullOrWhiteSpace(startLine))
            throw new InvalidOperationException("Empty request line");

        var (method, rawPath, version) = ParseStartLine(startLine);

        Headers headers = new();

        while (true)
        {
            var line = (await stream.ReadLineAsync() ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(line))
                return new Request
                {
                    Method = method,
                    RawPath = rawPath,
                    Version = version,
                    Headers = headers,
                    Body = Body.FromHeaders(stream, headers)
                };

            headers.Add(Header.From(line));
        }
    }
}