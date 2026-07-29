namespace Anidex.Services;

public class NotAllowedException : Exception
{
    public NotAllowedException(string message) : base(message)
    {
    }
}