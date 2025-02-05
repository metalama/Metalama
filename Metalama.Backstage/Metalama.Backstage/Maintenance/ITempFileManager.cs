// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Maintenance;

public interface ITempFileManager : IBackstageService
{
    /// <summary>
    /// Gets a temporary directory and creates it if it does not exist yet.
    /// </summary>
    /// <param name="directory">The principal name of the directory, before the version number.</param>
    /// <param name="cleanUpStrategy">The <see cref="CleanUpStrategy"/>.</param>
    /// <param name="subdirectory">An optional directory name after the version number.</param>
    /// <param name="versionScope"></param>
    /// <returns></returns>
    string GetTempDirectory(
        string directory,
        CleanUpStrategy cleanUpStrategy,
        string? subdirectory = null,
        TempFileVersionScope versionScope = TempFileVersionScope.Default );
}