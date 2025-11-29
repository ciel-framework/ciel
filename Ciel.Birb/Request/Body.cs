namespace Ciel.Birb;

public abstract class Body
{
    public static Body FromHeaders(Stream stream, Headers req)
    {
        var te = req.First(Header.TransferEncoding);
        if (te != null && te.Equals("chunked", StringComparison.OrdinalIgnoreCase))
            return new ChunkedBody(stream);

        return new ContentBody(stream, req.ContentLength ?? int.MaxValue);
    }

    public abstract ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default);

    public Stream ToStream()
    {
        return new BodyStreamAdaptor(this);
    }
}