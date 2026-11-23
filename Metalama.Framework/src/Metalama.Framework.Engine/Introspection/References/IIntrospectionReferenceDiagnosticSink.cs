// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Introspection.References;

/// <summary>
/// Receives the diagnostics that the introspection reference graph reports when the reference indexer skips syntax nodes
/// because of an exception. The host of the introspection API, such as a workspace, implements this service to show the
/// diagnostics to the user.
/// </summary>
/// <remarks>
/// When the service is not registered, the diagnostics are not shown, but the exceptions are still reported to the
/// exception handler of the project, when one is registered.
/// </remarks>
public interface IIntrospectionReferenceDiagnosticSink : IProjectService
{
    /// <summary>
    /// Reports the diagnostics produced while the references of a compilation were indexed.
    /// </summary>
    /// <param name="compilation">The compilation whose references were indexed.</param>
    /// <param name="diagnostics">The diagnostics. The array is not empty.</param>
    void Report( ICompilation compilation, ImmutableArray<Diagnostic> diagnostics );
}
