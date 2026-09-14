namespace TestVault.Application.Exceptions;

/// <summary>
/// The request conflicts with the current state of the system - e.g.
/// trying to start a manual run while one is already active. Ported from
/// the 409 branch in backend/routes/manual.js:10-14. Framework-agnostic -
/// TestVault.Web is responsible for translating this into a 409 response
/// (see ExceptionHandlingMiddleware).
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
