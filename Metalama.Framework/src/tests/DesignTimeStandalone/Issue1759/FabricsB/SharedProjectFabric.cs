// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;

namespace SharedFabrics;

/// <summary>
/// A transitive project fabric that has the same full name as the one declared by <c>FabricsA</c>.
/// </summary>
/// <remarks>
/// The two declarations are deliberately identical. Two independently published libraries that happen to declare a
/// fabric of the same namespace and name produce exactly this situation in a project that references both.
/// </remarks>
public class SharedProjectFabric : TransitiveProjectFabric
{
    /// <inheritdoc />
    public override void AmendProject( IProjectAmender amender ) { }
}
