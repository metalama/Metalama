// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Newtonsoft.Json;
using System.IO;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// Represent the content of the <c>metalamaTests.json</c> file. This class is JSON-serializable.
    /// </summary>
    public sealed class TestDirectoryOptions : TestOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the current directory and all child directories should be excluded.
        /// </summary>
        public bool? Exclude { get; set; }

        public bool? IsRoot { get; set; }

        internal static TestDirectoryOptions ReadFile( IFileSystem fileSystem, string path )
        {
            var json = fileSystem.ReadAllText( path );

            var options = JsonConvert.DeserializeObject<TestDirectoryOptions>( json )!;
            options.SetFullPaths( Path.GetDirectoryName( path )! );

            return options;
        }

        internal override void ApplyBaseOptions( TestDirectoryOptions baseOptions )
        {
            base.ApplyBaseOptions( baseOptions );

            this.Exclude ??= baseOptions.Exclude;
        }
    }
}