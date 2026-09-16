using System;
using System.IO;

namespace MediaConverter.Core.Services.Binaries;

/// <summary>
/// Resolves the folder where external binaries are stored next to the application.
/// </summary>
public static class BinaryPaths
{
    /// <summary>
    /// Gets the directory of the current process, preferring <see cref="Environment.ProcessPath"/>
    /// over <see cref="AppContext.BaseDirectory"/>. When the app is published as a self-contained
    /// single file, <see cref="AppContext.BaseDirectory"/> points to the temporary extraction
    /// directory, so the process path must be used instead.
    /// </summary>
    /// <returns>The absolute path of the directory containing the application executable.</returns>
    public static string GetExecutableDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            var directory = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrEmpty(directory))
            {
                return directory;
            }
        }

        return AppContext.BaseDirectory;
    }
}