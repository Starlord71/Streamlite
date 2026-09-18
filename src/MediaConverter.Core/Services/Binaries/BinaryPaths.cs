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

    /// <summary>
    /// Gets the folder where the application stores its user data, which today means the external
    /// binaries and the settings file. It lives in the user's local application data folder
    /// (<c>%LOCALAPPDATA%\MediaConverter</c> on Windows) so nothing is written next to the
    /// executable: the released application is a single file and the executable folder stays clean.
    /// </summary>
    /// <returns>
    /// The absolute path of the application data folder, falling back to the executable directory
    /// when the local application data folder is unavailable.
    /// </returns>
    public static string GetApplicationDataDirectory()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localApplicationData))
        {
            return GetExecutableDirectory();
        }

        return Path.Combine(localApplicationData, "MediaConverter");
    }
}