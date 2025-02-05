// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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