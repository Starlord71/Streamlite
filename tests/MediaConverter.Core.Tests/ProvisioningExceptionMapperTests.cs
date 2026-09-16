using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MediaConverter.Core.Models;
using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the mapping from provisioning exceptions to machine-readable error codes.
/// </summary>
public class ProvisioningExceptionMapperTests
{
    [Theory]
    [InlineData(typeof(OperationCanceledException), ErrorCode.Cancelled)]
    [InlineData(typeof(TaskCanceledException), ErrorCode.Cancelled)]
    [InlineData(typeof(UnauthorizedAccessException), ErrorCode.AccessDenied)]
    [InlineData(typeof(HttpRequestException), ErrorCode.NetworkError)]
    [InlineData(typeof(JsonException), ErrorCode.BinaryDownloadFailed)]
    [InlineData(typeof(IOException), ErrorCode.ProvisioningFailed)]
    [InlineData(typeof(PathTooLongException), ErrorCode.ProvisioningFailed)]
    [InlineData(typeof(InvalidDataException), ErrorCode.ProvisioningFailed)]
    [InlineData(typeof(ArgumentException), ErrorCode.ProvisioningFailed)]
    [InlineData(typeof(NotSupportedException), ErrorCode.ProvisioningFailed)]
    [InlineData(typeof(Exception), ErrorCode.Unknown)]
    public void Map_ExceptionTypes_ReturnsExpectedCode(Type exceptionType, ErrorCode expected)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Equal(expected, ProvisioningExceptionMapper.Map(exception));
    }
}