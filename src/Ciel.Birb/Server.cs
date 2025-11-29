using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Ciel.Birb;

public class Server(IHandler handler, ILogger logger, Configs configs)
{
    private async Task HandleClientAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var keepAlive = true;

        while (keepAlive)
        {
            var request = await Request.ReadHeaderAsync(stream);
            var connectionHeader = request.Headers.First(Header.Connection) ?? "keep-alive";
            if (connectionHeader == "close") keepAlive = false;

            logger.LogInformation("{Method} {RawPath} {Version}", request.Method, request.RawPath, request.Version);

            var response = new ResponseWriterImpl(stream);
            response.Headers.Add(Header.Server, configs.Name);

            await handler.HandleAsync(request, response);

            connectionHeader = response.Headers.First(Header.Connection) ?? "keep-alive";
            if (connectionHeader == "close") keepAlive = false;

            await stream.FlushAsync();
        }
    }

    public async Task ServeAsync()
    {
        var listener = TcpListener.Create(configs.Port);
        listener.Start();

        logger.LogInformation("{Configs} listening on http://{Endpoint}…", configs.Name, listener.LocalEndpoint);

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    public static Task ServeAsync(IHandler handler, Configs? configs = null)
    {
        configs ??= new Configs();
        var logger = configs.Logger;

        if (logger == null)
        {
            var factory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            logger = factory.CreateLogger(configs.Name);
        }

        return new Server(handler, logger, configs).ServeAsync();
    }

    public static Task ServeAsync(HandlerAsync handler, Configs? configs = null)
    {
        return ServeAsync(IHandler.From(handler), configs);
    }
}