using System.Text;

namespace Ciel.Birb;

public abstract class ResponseWriter
{
    public Status Status { get; set; } = Status.OK;
    public Headers Headers { get; } = new();

    public abstract Task SendHeaders(Status status);
    public abstract Task<int> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default);

    public Task WriteAsync(string text, CancellationToken ct = default)
    {
        return WriteAsync(Encoding.UTF8.GetBytes(text), ct);
    }

    public async Task NotFoundAsync()
    {
        Status = Status.NotFound;
        await WriteAsync("Not Found");
    }

    public async Task SendFileAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var buffer = await File.ReadAllBytesAsync(path, ct);
            await WriteAsync(buffer, ct);
        }
        catch (FileNotFoundException e)
        {
            await NotFoundAsync();
        }
    }

    public Stream ToStream()
    {
        return new ResponseStreamAdaptor(this);
    }
}