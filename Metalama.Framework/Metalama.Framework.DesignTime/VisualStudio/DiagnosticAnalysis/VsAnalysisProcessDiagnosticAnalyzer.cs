// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Framework.DesignTime.VisualStudio.DiagnosticAnalysis;

#pragma warning disable RS1001, RS1022, RS1025, RS1026

[UsedImplicitly]
[PublicAPI]
public sealed class VsAnalysisProcessDiagnosticAnalyzer : TheDiagnosticAnalyzer
{
    public VsAnalysisProcessDiagnosticAnalyzer( ServiceProvider<IGlobalService> serviceProvider ) : base( serviceProvider )
    {
        this._projectSourceGeneratorFactory = serviceProvider.GetRequiredService<VsAnalysisProcessProjectSourceGeneratorFactory>();
        this._projectOptionsFactory = serviceProvider.GetRequiredService<IProjectOptionsFactory>();
    }

    public VsAnalysisProcessDiagnosticAnalyzer() : this( DesignTimeServiceProviderFactory.GetSharedServiceProvider() ) { }

    private readonly VsAnalysisProcessProjectSourceGeneratorFactory _projectSourceGeneratorFactory;
    private readonly IProjectOptionsFactory _projectOptionsFactory;

    public override void Initialize( AnalysisContext context )
    {
        base.Initialize( context );

        // It seems that in packages.config projects, the source generator is not run reliably in the RoslynCodeAnalysisService process.
        // This is a problem, because the devenv generator normally depends on the RoslynCodeAnalysisService generator being run before.
        // To fix that, create VsAnalysisProcessProjectHandler, if it does not already exist,
        // which registers the project on the endpoint, the same as if the source generator was run.
        context.RegisterCompilationAction(
            compilationContext =>
            {
                var options = this._projectOptionsFactory.GetProjectOptions( compilationContext.Options.AnalyzerConfigOptionsProvider );

                if ( options is { IsFrameworkEnabled: true, IsDesignTimeEnabled: true } )
                {
                    var projectKey = compilationContext.Compilation.GetProjectKey();

                    if ( !projectKey.HasHashCode )
                    {
                        return;
                    }

                    this._projectSourceGeneratorFactory.GetOrCreateProjectHandler( options, projectKey );
                }
            } );
    }
}