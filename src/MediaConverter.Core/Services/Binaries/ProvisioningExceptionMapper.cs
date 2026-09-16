using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using MediaConverter.Core.Models;

namespace MediaConverter.Core.Services.Binaries;

/// <summary>
/// Maps exceptions raised while provisioning binaries to the closest <see cref="ErrorCode"/>.
/// </summary>
public static class ProvisioningExceptionMapper
{
    /// <summary>
    /// Maps an exception to an <see cref="ErrorCode"/>. Subclasses are checked before their
    /// base types; for example <see cref="UnauthorizedAccessException"/> derives from
    /// <see cref="IOException"/> and must map to <see cref="ErrorCode.AccessDenied"/>.
    /// </summary>
    /// <param name="exception">The exception raised by the provisioning logic.</param>
    /// <returns>The closest matching error code, or <see cref="ErrorCode.Unknown"/>.</returns>
    public static ErrorCode Map(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return ErrorCode.Cancelled;
        }

        if (exception is UnauthorizedAccessException)
        {
            return ErrorCode.AccessDenied;
        }

        if (exception is HttpRequestException)
        {
            return ErrorCode.NetworkError;
        }

        if (exception is JsonException)
        {
            return ErrorCode.BinaryDownloadFailed;
        }

        if (exception is IOException or InvalidDataException)
        {
            return ErrorCode.ProvisioningFailed;
        }

        if (exception is ArgumentException or NotSupportedException)
        {
            return ErrorCode.ProvisioningFailed;
        }

        return ErrorCode.Unknown;
    }
}