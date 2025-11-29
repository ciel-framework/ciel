using System.Globalization;

namespace Ciel.Birb;

internal class ChunkedBody(Stream stream) : Body
{
    private int _chunkOffset;
    private int _chunkSize;
    private bool _finished;
    private bool _haveChunk;


    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        if (buffer.Length == 0) return 0;
        if (_finished) return 0;

        // Need a new chunk?
        if (!_haveChunk || _chunkOffset >= _chunkSize)
        {
            var line = await stream.ReadLineAsync(ct) ?? string.Empty;
            var semi = line.IndexOf(';');
            var sizePart = semi >= 0 ? line[..semi] : line;

            if (!int.TryParse(sizePart.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _chunkSize))
                throw new FormatException($"Invalid chunk size: '{line}'");

            _chunkOffset = 0;
            _haveChunk = true;

            if (_chunkSize == 0)
            {
                // Consume trailers (ignored)
                while (true)
                {
                    var trailer = await stream.ReadLineAsync(ct);
                    if (string.IsNullOrEmpty(trailer))
                        break;
                }

                _finished = true;
                return 0;
            }
        }

        var remaining = _chunkSize - _chunkOffset;
        var toRead = Math.Min(remaining, buffer.Length);

        var read = await stream.ReadAsync(buffer.Slice(0, toRead), ct);
        if (read == 0) return 0;

        _chunkOffset += read;

        if (_chunkOffset == _chunkSize)
        {
            // Consume the CRLF after the chunk
            var crlf = new byte[2];
            var got = 0;
            while (got < 2)
            {
                var r = await stream.ReadAsync(crlf.AsMemory(got, 2 - got), ct);
                if (r == 0) break;
                got += r;
            }
        }

        return read;
    }
}