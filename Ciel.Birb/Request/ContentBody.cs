namespace Ciel.Birb;

internal class ContentBody(Stream stream, int contentLen) : Body
{
    private readonly int _contentLen = contentLen;
    private readonly Stream _stream = stream;
    private int _offset;

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        var remaining = _contentLen - _offset;
        if (remaining <= 0) return 0;

        var toRead = Math.Min(remaining, buffer.Length);
        if (toRead <= 0) return 0;

        var read = await _stream.ReadAsync(buffer.Slice(0, toRead), ct);
        if (read <= 0) return 0;

        _offset += read;
        return read;
    }
}