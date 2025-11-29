using System.Globalization;
using System.Text;

namespace Ciel.Birb;

internal class ResponseWriterImpl(Stream stream) : ResponseWriter
{
    private bool _headersSent;

    public override async Task SendHeaders(Status status)
    {
        if (_headersSent) return;
        _headersSent = true;

        // Default Date/Server if not already set
        if (Headers.First(Header.Date) is null)
            Headers.Add(Header.Date, DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));

        if (Headers.First(Header.Server) is null)
            Headers.Add(Header.Server, "Birb");

        var sb = new StringBuilder();
        sb.Append("HTTP/1.1 ");
        sb.Append(status.Code);
        sb.Append(' ');
        sb.Append(status.Message);
        sb.Append("\r\n");

        foreach (var h in Headers)
        {
            sb.Append(h.Key);
            sb.Append(": ");
            sb.Append(h.Value);
            sb.Append("\r\n");
        }

        sb.Append("\r\n");

        var bytes = Encoding.ASCII.GetBytes(sb.ToString());
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }

    public override async Task<int> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        // If the user forgot to send headers, be nice and send a 200
        if (!_headersSent)
        {
            Headers.ContentLength = buffer.Length;
            await SendHeaders(Status);
        }

        await stream.WriteAsync(buffer, ct);
        return buffer.Length;
    }
}