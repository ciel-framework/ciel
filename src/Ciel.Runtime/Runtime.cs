using Ciel.Birb;
using Ciel.Breeze;

namespace Ciel;

public static class Runtime
{
    public static async Task Main()
    {
        var tokens = Lexer.Tokenize("Hello World {{wow}} {%name%} {if true}wow{/if}");
        foreach (var token in tokens) Console.WriteLine(token);

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