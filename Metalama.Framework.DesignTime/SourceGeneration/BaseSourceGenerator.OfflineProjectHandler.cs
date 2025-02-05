// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Offline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.SourceGeneration;

public partial class BaseSourceGenerator
{
    private sealed class OfflineProjectHandler : ProjectSourceGenerator
    {
        private readonly GlobalServiceProvider _globalServiceProvider;
        private readonly ILogger _logger;

        public OfflineProjectHandler( GlobalServiceProvider serviceProvider, IProjectOptions projectOptions, ProjectKey projectKey ) : base(
            serviceProvider,
            projectOptions,
            projectKey )
        {
            this._globalServiceProvider = serviceProvider;
            this._logger = serviceProvider.GetLoggerFactory().GetLogger( "DesignTime" );
        }

        public override SourceGeneratorResult GenerateSources( Compilation compilation, TestableCancellationToken cancellationToken )
        {
            var serviceProvider = this._globalServiceProvider.Underlying.WithProjectScopedServices( this.ProjectOptions, compilation );

            var provider = new AdditionalCompilationOutputFileProvider( serviceProvider );

            if ( this.ProjectOptions.AdditionalCompilationOutputDirectory == null )
            {
                return SourceGeneratorResult.Empty;
            }

            var result = new OfflineSourceGeneratorResult(
                provider.GetAdditionalCompilationOutputFiles()
                    .Where(
                        f => f.Kind == AdditionalCompilationOutputFileKind.DesignTimeGeneratedCode
                             && StringComparer.Ordinal.Equals( Path.GetExtension( f.Path ), ".cs" ) )
                    .ToImmutableArray() );

            this._logger.Trace?.Log( $"DesignTimeSourceGenerator.Execute('{compilation.AssemblyName}'): {result.OfflineFiles.Length} sources generated." );

            return result;
        }
    }
}