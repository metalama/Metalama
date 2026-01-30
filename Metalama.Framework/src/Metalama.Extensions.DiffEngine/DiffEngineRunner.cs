// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using DiffEngine;
using Metalama.Testing.AspectTesting;

namespace Metalama.Extensions.DiffEngine;

/// <summary>
/// Implementation of <see cref="ISnapshotDiffToolRunner"/> that uses DiffEngine to launch diff tools.
/// </summary>
public sealed class DiffEngineRunner : ISnapshotDiffToolRunner
{
    /// <inheritdoc />
    public bool IsDisabled => DiffRunner.Disabled;

    /// <inheritdoc />
    public void SetMaxInstances( int count ) => DiffRunner.MaxInstancesToLaunch( count );

    /// <inheritdoc />
    public void Launch( string actualPath, string expectedPath ) => DiffRunner.Launch( actualPath, expectedPath );

    /// <inheritdoc />
    public void Kill( string actualPath, string expectedPath ) => DiffRunner.Kill( actualPath, expectedPath );
}
