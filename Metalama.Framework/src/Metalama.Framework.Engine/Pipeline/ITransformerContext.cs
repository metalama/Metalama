// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Pipeline;

internal interface ITransformerContext
{
    GlobalServiceProvider ServiceProvider { get; }

    void AddSyntaxTreeTransformations( IEnumerable<SyntaxTreeTransformation> transformations );

    Compilation Compilation { get; }

    IProjectOptions ProjectOptions { get; }

    ImmutableArray<ManagedResource> Resources { get; }

    void ReportDiagnostic( Diagnostic diagnostic );

    void AddResources( IEnumerable<ManagedResource> resources );

    void RegisterDiagnosticFilter( in DiagnosticFilter filter );
}