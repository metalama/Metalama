// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// Reads and caches the <c>metalamaTests.json</c> files.
    /// </summary>
    internal sealed class TestDirectoryOptionsReader
    {
        private readonly IFileSystem _fileSystem;

        public string ProjectDirectory { get; }

        private readonly ConcurrentDictionary<string, TestDirectoryOptions> _cache = new( StringComparer.OrdinalIgnoreCase );

        public TestDirectoryOptionsReader( GlobalServiceProvider serviceProvider, string projectDirectory )
        {
            this.ProjectDirectory = projectDirectory;
            this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        }

        public TestDirectoryOptions GetDirectoryOptions( string directory ) => this._cache.GetOrAdd( directory, this.GetDirectoryOptionsImpl );

        private TestDirectoryOptions GetDirectoryOptionsImpl( string directory )
        {
            // Read the json file in the directory.
            var optionsPath = Path.Combine( directory, "metalamaTests.json" );

            var options = this._fileSystem.FileExists( optionsPath )
                ? TestDirectoryOptions.ReadFile( this._fileSystem, optionsPath )
                : new TestDirectoryOptions();

            if ( options.IsRoot != true )
            {
                // Apply settings from the parent directory.
                var parentDirectory = Path.GetDirectoryName( directory );

                if ( parentDirectory != null )
                {
                    var baseOptions = this.GetDirectoryOptions( parentDirectory );
                    options.ApplyBaseOptions( baseOptions );
                }
            }

            return options;
        }
    }
}