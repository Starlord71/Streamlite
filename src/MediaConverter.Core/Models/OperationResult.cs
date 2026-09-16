namespace MediaConverter.Core.Models;

/// <summary>
/// Outcome of a Core operation. Success is represented by <see cref="Ok"/>
/// and failures by a machine-readable <see cref="ErrorCode"/> that the UI
/// layer localizes. Core never returns user-facing strings.
/// </summary>
public sealed record OperationResult
{
    /// <summary>Gets a value indicating whether the operation completed successfully.</summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the error code for a failed operation, or <see langword="null"/> when it succeeded.
    /// </summary>
    public ErrorCode? ErrorCode { get; private init; }

    /// <summary>Creates a successful result.</summary>
    /// <returns>An <see cref="OperationResult"/> with <see cref="Succeeded"/> set to <see langword="true"/>.</returns>
    public static OperationResult Ok() => new() { Succeeded = true };

    /// <summary>Creates a failed result.</summary>
    /// <param name="errorCode">The code identifying the failure.</param>
    /// <returns>An <see cref="OperationResult"/> with <see cref="Succeeded"/> set to <see langword="false"/> and the given <paramref name="errorCode"/>.</returns>
    public static OperationResult Fail(ErrorCode errorCode) =>
        new() { Succeeded = false, ErrorCode = errorCode };
}