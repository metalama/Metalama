// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;

namespace SharedFabrics;

/// <summary>
/// A transitive project fabric that has the same full name as the one declared by <c>FabricsB</c>.
/// </summary>
/// <remarks>
/// <para>
/// The fabric amends nothing. The defect occurs while the driver of the fabric is created, which happens before any
/// fabric is executed, so the body of <see cref="AmendProject"/> is irrelevant to the reproduction.
/// </para>
/// <para>
/// The type declaration survives in the run-time assembly, because <c>RunTimeAssemblyRewriter</c> replaces the bodies
/// of compile-time members by a throw statement and keeps their declarations. Both <c>FabricsA</c> and <c>FabricsB</c>
/// therefore export a type of this full name, which is what makes the name ambiguous in the consumer.
/// </para>
/// </remarks>
public class SharedProjectFabric : TransitiveProjectFabric
{
    /// <inheritdoc />
    public override void AmendProject( IProjectAmender amender ) { }
}
