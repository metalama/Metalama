// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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