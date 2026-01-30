// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// Provides diff tool functionality for the test framework.
/// When no implementation is available (i.e., when the <c>Metalama.Extensions.DiffEngine</c> package is not referenced),
/// diff tool features are silently disabled and tests continue to work normally.
/// </summary>
/// <remarks>
/// <para>
/// To enable diff tool support, add a reference to the <c>Metalama.Extensions.DiffEngine</c> package.
/// </para>
/// </remarks>
[PublicAPI]
public interface IDiffToolRunner
{
    /// <summary>
    /// Gets a value indicating whether the diff tool is disabled.
    /// </summary>
    bool IsDisabled { get; }

    /// <summary>
    /// Sets the maximum number of diff tool instances that can be launched concurrently.
    /// </summary>
    /// <param name="count">The maximum number of instances.</param>
    void SetMaxInstances( int count );

    /// <summary>
    /// Launches a diff tool to compare two files.
    /// </summary>
    /// <param name="actualPath">The path to the actual (generated) file.</param>
    /// <param name="expectedPath">The path to the expected file.</param>
    void Launch( string actualPath, string expectedPath );

    /// <summary>
    /// Kills any diff tool instance that was launched to compare the specified files.
    /// This is typically called when the files become equal after an update.
    /// </summary>
    /// <param name="actualPath">The path to the actual (generated) file.</param>
    /// <param name="expectedPath">The path to the expected file.</param>
    void Kill( string actualPath, string expectedPath );
}