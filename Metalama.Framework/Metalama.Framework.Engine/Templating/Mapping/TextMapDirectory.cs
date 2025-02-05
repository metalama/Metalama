// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Metalama.Framework.Engine.Templating.Mapping
{
    /// <summary>
    /// Represents a set of <see cref="TextMapFile"/>.
    /// </summary>
    internal sealed class TextMapDirectory : ITextMapFileProvider
    {
        public ImmutableDictionary<string, TextMapFile> FilesByTargetPath { get; }

        private TextMapDirectory( ImmutableDictionary<string, TextMapFile> filesByTargetPath )
        {
            this.FilesByTargetPath = filesByTargetPath;
        }

        /// <summary>
        /// Loads a new instance of <see cref="TextMapDirectory"/> from the file system.
        /// </summary>
        public static TextMapDirectory Load( string? directory )
        {
            var dictionaryBuilder = ImmutableDictionary.CreateBuilder<string, TextMapFile>();

            foreach ( var filePath in Directory.GetFiles( directory!, "*.map" ) )
            {
                var file = TextMapFile.Read( filePath );

                if ( file != null )
                {
                    dictionaryBuilder.Add( file.TargetPath, file );
                }
            }

            return new TextMapDirectory( dictionaryBuilder.ToImmutable() );
        }

        /// <summary>
        /// Creates a new <see cref="TextMapDirectory"/> from a transformed <see cref="Compilation"/> and the <see cref="ILocationAnnotationMap"/>
        /// with which the compilation has been annotated before transformation.
        /// </summary>
        public static TextMapDirectory Create( Compilation compileTimeCompilation, ILocationAnnotationMap locationAnnotationMap )
        {
            var files = compileTimeCompilation.SyntaxTrees.Select( t => TextMapFile.Create( t, locationAnnotationMap ) ).WhereNotNull();

            return new TextMapDirectory( files.ToImmutableDictionary( f => f.TargetPath, f => f ) );
        }

        /// <summary>
        /// Writes the content of the current <see cref="TextMapDirectory"/> to the filesystem.
        /// </summary>
        public void Write( string outputDirectory )
        {
            foreach ( var map in this.FilesByTargetPath.Values )
            {
                var filePath = Path.Combine( outputDirectory, Path.GetFileNameWithoutExtension( map.TargetPath ) + ".map" );

                using ( var writer = File.Create( filePath ) )
                {
                    map.Write( writer );
                }
            }
        }

        public bool TryGetMapFile( string path, [NotNullWhen( true )] out TextMapFile? file )
        {
            file = this.FilesByTargetPath.Values.Where( m => path.EndsWith( m.TargetPath, StringComparison.OrdinalIgnoreCase ) )
                .MaxByOrNull( m => m.TargetPath.Length );

            return file != null;
        }
    }
}