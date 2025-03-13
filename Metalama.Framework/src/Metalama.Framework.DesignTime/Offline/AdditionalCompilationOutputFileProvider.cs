// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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