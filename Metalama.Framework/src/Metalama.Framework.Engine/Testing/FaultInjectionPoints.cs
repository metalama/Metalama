// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Testing
{
    /// <summary>
    /// The names of the fault injection points used with <see cref="ITestFaultInjector"/>. See #1701.
    /// </summary>
    internal static class FaultInjectionPoints
    {
        /// <summary>
        /// Injected at the start of <c>SourceTransformer.Execute</c>, before the per-project service scope is
        /// established, to exercise the global (outer) exception-handling layer that reports tooling telemetry.
        /// </summary>
        public const string SourceTransformerEntry = "SourceTransformer.Entry";

        /// <summary>
        /// Injected inside <c>SourceTransformer.Execute</c> once the per-project service scope is established, to
        /// exercise the project-scoped (inner) exception-handling layer that reports repository telemetry.
        /// </summary>
        public const string CompileTimePipeline = "SourceTransformer.CompileTimePipeline";
    }
}
