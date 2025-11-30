namespace Ciel.Birb;

public interface IHandler
{
    public static IHandler From(HandlerAsync handlerAsync)
    {
        return new HandlerFuncWrapper(handlerAsync);
    }

    Task HandleAsync(Request req, ResponseWriter resp);
}