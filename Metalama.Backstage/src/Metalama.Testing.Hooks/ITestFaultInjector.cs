// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Testing.Hooks;

/// <summary>
/// Provides fault injection points, so that a test can deterministically make production code throw at a chosen
/// place, in order to exercise its exception handling.
/// </summary>
/// <remarks>
/// <para>
/// This service is optional and is never registered in production. When it is absent, an injection point is a no-op,
/// exactly like a synchronization point of <see cref="ITestSynchronizationProvider"/>.
/// </para>
/// <para>
/// The names of the injection points are declared by each layer, close to the code that reaches them, because they
/// are meaningful only there.
/// </para>
/// </remarks>
/// <seealso cref="TestFaultInjector"/>
public interface ITestFaultInjector
{
    /// <summary>
    /// Called by the code under test at a named injection point. Throws the exception armed for
    /// <paramref name="injectionPointName"/>, if any, and otherwise returns without effect.
    /// </summary>
    /// <param name="injectionPointName">A unique name identifying this injection point.</param>
    void InjectFault( string injectionPointName );
}
