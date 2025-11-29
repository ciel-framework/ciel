using Ciel.Birb;
using Ciel.Birb.Extra;

namespace Ciel;

public static class Runtime
{
    public static async Task Main()
    {
        await Server.ServeAsync(new FileServer(Directory.GetCurrentDirectory(), true));
    }
}