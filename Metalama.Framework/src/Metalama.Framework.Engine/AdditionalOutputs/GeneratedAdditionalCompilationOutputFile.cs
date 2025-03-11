// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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