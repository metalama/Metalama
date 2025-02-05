// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.IO;

namespace Metalama.Framework.Engine.AdditionalOutputs
{
    public abstract class AdditionalCompilationOutputFile
    {
        public abstract AdditionalCompilationOutputFileKind Kind { get; }

        public abstract string Path { get; }

        public abstract void WriteToStream( Stream stream );

        public abstract Stream GetStream();
    }
}