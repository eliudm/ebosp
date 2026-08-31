namespace EBOSP.Application.Common;

/// <summary>
/// Small, typed exceptions that <see cref="EBOSP.Api.Middleware.GlobalExceptionHandler"/> maps to
/// specific HTTP status codes, so use cases can signal an outcome without depending on ASP.NET
/// Core status codes directly.
/// </summary>
public sealed class AuthenticationFailedException() : Exception("Authentication failed.");

public sealed class ForbiddenOperationException(string message) : Exception(message);

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);
