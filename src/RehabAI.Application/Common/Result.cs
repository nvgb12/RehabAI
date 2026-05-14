namespace RehabAI.Application.Common;

public sealed record Result(bool Succeeded, string Message)
{
    public static Result Success(string message = "Success") => new(true, message);
    public static Result Failure(string message) => new(false, message);
}
