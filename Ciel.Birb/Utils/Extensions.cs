using System.Text;

namespace Ciel.Birb;

public static class StreamExtensions
{
    public static async Task<string?> ReadLineAsync(this Stream stream, CancellationToken ct = new())
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead)
            throw new InvalidOperationException("Stream is not readable.");

        var sb = new StringBuilder();
        var buffer = new byte[1];

        while (true)
        {
            var read = await stream.ReadAsync(buffer, 0, 1, ct).ConfigureAwait(false);

            if (read == 0) return sb.Length == 0 ? null : sb.ToString();

            var b = buffer[0];

            if (b == (byte)'\n')
                break;
            if (b == (byte)'\r')
                continue;

            sb.Append((char)b);
        }

        return sb.ToString();
    }
}