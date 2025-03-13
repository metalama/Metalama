// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Telemetry
{
    /// <summary>
    /// Represents what kind of issue is being reported.
    /// </summary>
    [PublicAPI]
    public enum ExceptionReportingKind
    {
        /// <summary>
        /// An actual exception, other than VS extension's MainThreadBlockedException, is being reported.
        /// </summary>
        Exception,

        /// <summary>
        /// The main thread of Visual Studio is blocked for a long time.
        /// </summary>
        PerformanceProblem
    }
}