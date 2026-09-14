namespace TestVault.Application.Exceptions;

/// <summary>
/// Wraps a low-level data-access failure (e.g. a SqlException) raised by a
/// TestVault.Infrastructure repository. Exists so callers in this project -
/// and any future TestVault.Application service - can catch a single,
/// meaningful exception type without referencing TestVault.Infrastructure
/// or an ADO.NET provider directly (Application must not depend on
/// Infrastructure).
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
