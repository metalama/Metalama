// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Offline
{
    internal sealed class AdditionalCompilationOutputFileProvider : IAdditionalOutputFileProvider
    {
        private readonly ProjectServiceProvider _serviceProvider;

        public AdditionalCompilationOutputFileProvider( ServiceProvider<IProjectService> serviceProvider )
        {
            this._serviceProvider = serviceProvider;
        }

        public ImmutableArray<AdditionalCompilationOutputFile> GetAdditionalCompilationOutputFiles()
        {
            var projectOptions = this._serviceProvider.GetService<IProjectOptions>();

            if ( projectOptions?.AdditionalCompilationOutputDirectory == null )
            {
                return ImmutableArray<AdditionalCompilationOutputFile>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<AdditionalCompilationOutputFile>();

            foreach ( var kindDirectory in Directory.GetDirectories( projectOptions.AdditionalCompilationOutputDirectory ) )
            {
                if ( !Enum.TryParse<AdditionalCompilationOutputFileKind>( Path.GetFileName( kindDirectory ), out var kind ) )
                {
                    continue;
                }

                var kindDirectoryNormalized = Path.GetFullPath( kindDirectory );

                foreach ( var file in Directory.GetFiles( kindDirectory, "*", SearchOption.AllDirectories ) )
                {
                    // TODO: This is probably not reliable.
                    var fileNormalized = Path.GetFullPath( file );
                    var relativePath = fileNormalized.Substring( kindDirectoryNormalized.Length + 1 );

                    builder.Add( new ExistingAdditionalCompilationOutputFile( projectOptions.AdditionalCompilationOutputDirectory, kind, relativePath ) );
                }
            }

            return builder.ToImmutable();
        }
    }
}