namespace Ciel.Birb;

public sealed class Router : IHandler
{
    private readonly List<(RoutePattern Pattern, IHandler Handler)> _routes = new();

    // Async::Task<> handleAsync(Rc<Request> req, Rc<Response::Writer> resp) override
    public async Task HandleAsync(Request req, ResponseWriter resp)
    {
        foreach (var (pattern, handler) in _routes)
        {
            var matchResult = pattern.Match(req.Method, req.Url);
            if (matchResult != null)
            {
                req.PathParams = matchResult.PathParams;
                req.HostParams = matchResult.HostParams;
                await handler.HandleAsync(req, resp);
                return;
            }
        }

        await resp.NotFoundAsync();
    }

    public void Route(RoutePattern pattern, IHandler handler)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _routes.Add((pattern, handler));
    }

    public void Route(RoutePattern pattern, HandlerAsync handlerFunc)
    {
        Route(pattern, IHandler.From(handlerFunc));
    }

    public void Route(string pattern, IHandler handler)
    {
        Route(RoutePattern.Parse(pattern), handler);
    }

    public void Route(string pattern, HandlerAsync handlerFunc)
    {
        Route(RoutePattern.Parse(pattern), IHandler.From(handlerFunc));
    }

    public void Get(string pattern, IHandler handler)
    {
        Route(RoutePattern.Parse(Method.Get, pattern), handler);
    }

    public void Get(string pattern, HandlerAsync handlerFunc)
    {
        Route(RoutePattern.Parse(Method.Get, pattern), IHandler.From(handlerFunc));
    }

    public void Post(string pattern, IHandler handler)
    {
        Route(RoutePattern.Parse(Method.Post, pattern), handler);
    }

    public void Post(string pattern, HandlerAsync handlerFunc)
    {
        Route(RoutePattern.Parse(Method.Post, pattern), IHandler.From(handlerFunc));
    }

    public void Put(string pattern, IHandler handler)
    {
        Route(RoutePattern.Parse(Method.Put, pattern), handler);
    }

    public void Put(string pattern, HandlerAsync handlerFunc)
    {
        Route(RoutePattern.Parse(Method.Put, pattern), IHandler.From(handlerFunc));
    }

    public void Delete(string pattern, IHandler handler)
    {
        Route(RoutePattern.Parse(Method.Delete, pattern), handler);
    }

    public void Delete(string pattern, HandlerAsync handlerFunc)
    {
        Route(RoutePattern.Parse(Method.Delete, pattern), IHandler.From(handlerFunc));
    }

    private static IHandler MakeHandler(HandlerAsync func)
    {
        return new DelegateHandler(func);
    }


    private sealed class DelegateHandler : IHandler
    {
        private readonly HandlerAsync _func;

        public DelegateHandler(HandlerAsync func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public Task HandleAsync(Request req, ResponseWriter resp)
        {
            return _func(req, resp);
        }
    }
}