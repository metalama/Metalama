// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

internal interface ICompileTimeAssemblyLocatorProvider : IGlobalService
{
    /// <summary>
    /// Gets the <see cref="CompileTimeAssemblyLocator"/> of a project, creating it if necessary, and reports to
    /// <paramref name="diagnostics"/> and returns <c>false</c> when it cannot be created.
    /// </summary>
    /// <remarks>
    /// This is the entry point of every caller that has a diagnostic sink. Only a successfully created locator is
    /// cached, so a project whose environment is repaired succeeds on the next compilation.
    /// </remarks>
    bool TryGetInstance(
        in ProjectServiceProvider serviceProvider,
        IDiagnosticAdder diagnostics,
        [NotNullWhen( true )] out CompileTimeAssemblyLocator? locator );

    /// <summary>
    /// Gets the <see cref="CompileTimeAssemblyLocator"/> of a project, creating it if necessary and throwing a
    /// <see cref="DiagnosticException"/> when it cannot be created.
    /// </summary>
    /// <remarks>
    /// For the callers that have no diagnostic sink. A caller on a pipeline path must use
    /// <see cref="TryGetInstance"/> instead, so that the failure becomes a diagnostic of that pipeline.
    /// </remarks>
    CompileTimeAssemblyLocator GetInstance( in ProjectServiceProvider serviceProvider );
}