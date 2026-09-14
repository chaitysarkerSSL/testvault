namespace TestVault.Application.Exceptions;

/// <summary>
/// Login credentials were rejected, or a refresh token is invalid, expired,
/// or already revoked. Maps to HTTP 401 (see ExceptionHandlingMiddleware) -
/// distinct from ASP.NET Core's own built-in 401 for a missing/invalid
/// bearer token on an [Authorize] endpoint (that's handled entirely by the
/// JWT authentication middleware, before a controller action ever runs).
/// This exception is for AuthController's own actions - POST /api/auth/login
/// and /refresh - rejecting a request on its merits.
/// </summary>
public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message)
    {
    }
}
