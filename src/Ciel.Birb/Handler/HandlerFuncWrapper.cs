namespace Ciel.Birb;

internal class HandlerFuncWrapper(HandlerAsync handlerAsync) : IHandler
{
    public Task HandleAsync(Request req, ResponseWriter resp)
    {
        return handlerAsync(req, resp);
    }
}