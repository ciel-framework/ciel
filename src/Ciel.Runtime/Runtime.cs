using Ciel.Birb;

namespace Ciel;

public static class Runtime
{
    public static async Task Main()
    {
        Router router = new();

        // router.Get("/hello", async (req, resp) => { await resp.WriteAsync("Hello World!"); });
        router.Get("{user}.* /blog/{article}",
            async (req, resp) =>
            {
                await resp.WriteAsync(
                    $"This is {req.HostParams["user"]}!\nAnd you are reading about {req.PathParams["article"]}");
            });
        router.Get("/drive/{path...}", new Static("/", true));

        await Server.ServeAsync(router);
    }
}