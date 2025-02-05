// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

    public void AddSyntaxTreeTransformations( IEnumerable<SyntaxTreeTransformation> transformations );

    public Compilation Compilation { get; }

    IProjectOptions ProjectOptions { get; }

    public ImmutableArray<ManagedResource> Resources { get; }

    public void ReportDiagnostic( Diagnostic diagnostic );

    public void AddResources( IEnumerable<ManagedResource> resources );

    public void RegisterDiagnosticFilter( in DiagnosticFilter filter );
}