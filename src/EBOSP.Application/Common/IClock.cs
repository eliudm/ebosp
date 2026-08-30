namespace EBOSP.Application.Common;

/// <summary>
/// Abstraction over the current time so time-dependent business rules are testable
/// (dev guide §9, foundation module).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
