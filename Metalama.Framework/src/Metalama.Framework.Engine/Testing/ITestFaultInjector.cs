// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Testing
{
    /// <summary>
    /// An optional, test-only service that lets tests deterministically inject faults (thrown exceptions) at named
    /// injection points in the engine. It mirrors <c>ITestSynchronizationProvider</c>: it is never registered in
    /// production, and when the service is absent an injection point is a no-op. Used to exercise the engine's exception
    /// handling end-to-end (e.g. the two-layer handling in <c>SourceTransformer.Execute</c>). See #1701.
    /// </summary>
    internal interface ITestFaultInjector : IGlobalService
    {
        /// <summary>
        /// Called by engine code at a named injection point. Throws the exception armed for
        /// <paramref name="injectionPointName"/>, if any; otherwise returns without effect.
        /// </summary>
        /// <param name="injectionPointName">A unique name identifying the injection point. See <see cref="FaultInjectionPoints"/>.</param>
        void InjectFault( string injectionPointName );
    }
}
