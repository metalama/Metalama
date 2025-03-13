// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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