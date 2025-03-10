// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

internal sealed class TestTransformerContext : ITransformerContext
{
    public TestTransformerContext( Compilation compilation, IProjectOptions projectOptions, GlobalServiceProvider serviceProvider )
    {
        this.Compilation = compilation;
        this.ProjectOptions = projectOptions;
        this.ServiceProvider = serviceProvider;
    }
    
    public DiagnosticFilterCollection ReportedSuppressions { get; } = new();

    public GlobalServiceProvider ServiceProvider { get; }

    public void AddSyntaxTreeTransformations( IEnumerable<SyntaxTreeTransformation> transformations ) { }

    public Compilation Compilation { get; }

    public IProjectOptions ProjectOptions { get; }

    public ImmutableArray<ManagedResource> Resources => ImmutableArray<ManagedResource>.Empty;

    public void ReportDiagnostic( Diagnostic diagnostic ) { }

    public void AddResources( IEnumerable<ManagedResource> resources ) { }

    public void RegisterDiagnosticFilter( in DiagnosticFilter filter ) => this.ReportedSuppressions.Add( filter );
}