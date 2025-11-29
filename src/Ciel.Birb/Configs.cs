using Microsoft.Extensions.Logging;

namespace Ciel.Birb;

public class Configs
{
    public string Name { get; init; } = "Birb";
    public ushort Port { get; init; } = 8080;

    public ILogger? Logger { get; init; } = null;
}