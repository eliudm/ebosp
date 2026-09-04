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

public sealed class MfaChallengeRequiredException() : Exception("MFA challenge required.");

/// <summary>A feature is present but not currently usable (e.g. the AI assistant with no configured API key) - distinct from NotFoundException, since the endpoint itself exists.</summary>
public sealed class ServiceUnavailableException(string message) : Exception(message);

/// <summary>
/// Translated from EF Core's DbUpdateConcurrencyException at the Infrastructure boundary so the
/// Application layer never depends on EF Core directly (see AppDbContext.SaveChangesAsync).
/// </summary>
public sealed class ConcurrencyConflictException(Exception inner) : Exception("The record was modified by another operation. Retry.", inner);
