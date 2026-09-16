// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Repositories;

internal enum RepositoryConfigurationWarningKind
{
    /// <summary>
    /// A <c>metalama.json</c> was found outside the repository root and was ignored.
    /// </summary>
    MisplacedFile,

    /// <summary>
    /// A <c>metalama.json</c> exists but could not be parsed and was ignored.
    /// </summary>
    MalformedFile
}
