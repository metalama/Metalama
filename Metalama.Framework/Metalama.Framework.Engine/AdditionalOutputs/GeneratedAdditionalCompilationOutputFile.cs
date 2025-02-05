// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;

namespace Metalama.Framework.Engine.AdditionalOutputs
{
    internal sealed class GeneratedAdditionalCompilationOutputFile : AdditionalCompilationOutputFile
    {
        public override string Path { get; }

        public override AdditionalCompilationOutputFileKind Kind { get; }

        private readonly Action<Stream> _writeAction;

        public GeneratedAdditionalCompilationOutputFile( string path, AdditionalCompilationOutputFileKind kind, Action<Stream> writeAction )
        {
            this.Path = path;
            this.Kind = kind;
            this._writeAction = writeAction;
        }

        public override Stream GetStream()
        {
            throw new NotSupportedException();
        }

        public override void WriteToStream( Stream stream )
        {
            this._writeAction( stream );
        }
    }
}