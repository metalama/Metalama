// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.Classification;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Framework.DesignTime.VisualStudio.Classification;

internal sealed class DesignTimeClassificationService : IClassificationService, IDisposable
{
    private readonly ServiceProvider<IGlobalService> _serviceProvider;
    private readonly IMetalamaProjectClassifier _projectClassifier;
    private readonly WeakCache<IProjectOptions, ClassificationService> _projectClassificationServices = new();

    private readonly MSBuildProjectOptionsFactory _msBuildProjectOptionsFactory = new( [MSBuildPropertyNames.MetalamaCompileTimePackages] );

    public DesignTimeClassificationService( ServiceProvider<IGlobalService> serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._projectClassifier = serviceProvider.GetRequiredService<IMetalamaProjectClassifier>();
    }

    public bool ContainsCompileTimeCode( SyntaxNode syntaxRoot ) => ClassificationService.ContainsCompileTimeCode( syntaxRoot );

    public IDesignTimeClassifiedTextCollection GetClassifiedTextSpans(
        SemanticModel semanticModel,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        CancellationToken cancellationToken )
    {
        if ( semanticModel.Compilation.ExternalReferences.IsDefaultOrEmpty
             || !this._projectClassifier.TryGetMetalamaVersion( semanticModel.Compilation, out _ ) )
        {
            // Do not return anything if the compilation is not initialized or is not a Metalama project.
            return EmptyDesignTimeClassifiedTextCollection.Instance;
        }

        var projectOptions = this._msBuildProjectOptionsFactory.GetProjectOptions( analyzerConfigOptionsProvider );

        var classificationService = this._projectClassificationServices.GetOrAdd( projectOptions, this.CreateClassificationService );

        return new DesignTimeClassifiedTextSpansCollection( classificationService.GetClassifiedTextSpans( semanticModel, cancellationToken ) );
    }

    private ClassificationService CreateClassificationService( IProjectOptions options )
    {
        return new ClassificationService( this._serviceProvider.WithProjectScopedServices( options, Array.Empty<PortableExecutableReference>() ) );
    }

    public void Dispose() => this._projectClassificationServices.Dispose();
}