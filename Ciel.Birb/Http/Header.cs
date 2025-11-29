namespace Ciel.Birb;

public struct Header(string key, string value)
{
    public const string ContentLength = "Content-Length";
    public const string ContentType = "Content-Type";
    public const string ContentEncoding = "Content-Encoding";
    public const string ContentLanguage = "Content-Language";
    public const string ContentDisposition = "Content-Disposition";

    public const string Accept = "Accept";
    public const string AcceptEncoding = "Accept-Encoding";
    public const string AcceptLanguage = "Accept-Language";

    public const string Host = "Host";
    public const string UserAgent = "User-Agent";
    public const string Referer = "Referer";
    public const string Origin = "Origin";

    public const string CacheControl = "Cache-Control";
    public const string Pragma = "Pragma";
    public const string Expires = "Expires";

    public const string Authorization = "Authorization";
    public const string WwwAuthenticate = "WWW-Authenticate";

    public const string Cookie = "Cookie";
    public const string SetCookie = "Set-Cookie";

    public const string Connection = "Connection";
    public const string Upgrade = "Upgrade";
    public const string TransferEncoding = "Transfer-Encoding";
    public const string Te = "TE";

    public const string Range = "Range";
    public const string ContentRange = "Content-Range";

    public const string Location = "Location";

    public const string ETag = "ETag";
    public const string IfNoneMatch = "If-None-Match";
    public const string IfModifiedSince = "If-Modified-Since";

    public const string Date = "Date";
    public const string Server = "Server";
    public const string Via = "Via";

    public string Key = key;
    public string Value = value;

    public static Header From(string line)
    {
        var idx = line.IndexOf(':');
        if (idx <= 0)
            throw new FormatException($"Invalid header line: {line}");

        var key = line[..idx].Trim();
        var value = line[(idx + 1)..].Trim();
        return new Header(key, value);
    }
}