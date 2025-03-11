// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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