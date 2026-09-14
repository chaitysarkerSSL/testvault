namespace TestVault.Application.Exceptions;

/// <summary>
/// A business rule was violated by the request (e.g. trying to AI-analyze a
/// test case that didn't fail). Ported from the 400 branch in
/// backend/routes/analysis.js. Framework-agnostic - TestVault.Web is
/// responsible for translating this into a 400 response once controllers
/// exist.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
