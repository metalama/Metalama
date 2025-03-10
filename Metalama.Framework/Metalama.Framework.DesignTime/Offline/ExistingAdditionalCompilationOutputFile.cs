// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AdditionalOutputs;

namespace Metalama.Framework.DesignTime.Offline
{
    internal sealed class ExistingAdditionalCompilationOutputFile : AdditionalCompilationOutputFile
    {
        private readonly string _additionalCompilationOutputDirectory;

        public override string Path { get; }

        public override AdditionalCompilationOutputFileKind Kind { get; }

        public ExistingAdditionalCompilationOutputFile( string additionalCompilationOutputDirectory, AdditionalCompilationOutputFileKind kind, string path )
        {
            this._additionalCompilationOutputDirectory = additionalCompilationOutputDirectory;
            this.Path = path;
            this.Kind = kind;
        }

        public override Stream GetStream()
        {
            var path = System.IO.Path.Combine( this._additionalCompilationOutputDirectory, this.Kind.ToString(), this.Path );

            return File.OpenRead( path );
        }

        public override void WriteToStream( Stream stream )
        {
            throw new NotSupportedException();
        }
    }
}