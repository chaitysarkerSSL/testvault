namespace TestVault.Application.Exceptions;

/// <summary>
/// A requested entity does not exist. Ported from the 404 branches in
/// backend/routes/runs.js and backend/routes/analysis.js. Framework-agnostic
/// by design - it carries no HTTP status code; TestVault.Web is responsible
/// for translating this into a 404 response once controllers exist.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
