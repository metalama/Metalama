// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// Handles a compile-time exception: writes a local report, reports a diagnostic and captures telemetry through the
    /// per-project context (honoring the repository <c>metalama.json</c> opt-out). This is a project-scoped service, so it
    /// resolves the project options it needs from its own service provider. See #1701.
    /// </summary>
    internal interface ICompileTimeExceptionHandler : IProjectService
    {
        void ReportException( Exception exception, Action<Diagnostic> reportDiagnostic, bool canIgnoreException, out bool isHandled );
    }
}
