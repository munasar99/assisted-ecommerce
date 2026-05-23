namespace AssistedEcommerce.Api.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException(string message) : ApiException(message, 404);

public class UnauthorizedAppException(string message) : ApiException(message, 401);
