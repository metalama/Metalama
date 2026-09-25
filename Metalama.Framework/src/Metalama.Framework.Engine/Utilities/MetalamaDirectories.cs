// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using SharpCrafters.Backstage.Infrastructure;
using SharpCrafters.Backstage.Utilities;

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// Provides the directories in which Metalama writes its temporary files.
/// </summary>
/// <remarks>
/// The directories come from <see cref="IStandardDirectories"/>, which applies the <c>METALAMA_TEMP</c> environment
/// variable and avoids the world-writable <c>/tmp</c> directory on Unix. A directory that must be cleaned up when it is
/// no longer used is obtained from <c>ITempFileManager</c> instead.
/// </remarks>
public sealed class MetalamaDirectories : IGlobalService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetalamaDirectories"/> class.
    /// </summary>
    /// <param name="standardDirectories">The standard directories of the Backstage services.</param>
    public MetalamaDirectories( IStandardDirectories standardDirectories )
    {
        this.TempDirectory = standardDirectories.TempDirectory;
    }

    /// <summary>
    /// Gets the root of the temporary directory of Metalama.
    /// </summary>
    public string TempDirectory { get; }

    /// <summary>
    /// Creates an empty file with a unique name in <see cref="TempDirectory"/>.
    /// </summary>
    /// <returns>The full path of the file.</returns>
    public string GetTempFileName() => TempFileUtilities.GetTempFileName( this.TempDirectory );
}
