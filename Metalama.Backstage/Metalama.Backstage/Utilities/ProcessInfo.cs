// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.IO;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Utilities
{
    public sealed class ProcessInfo
    {
        public int ProcessId { get; }

        public string? ImagePath { get; }

        public string? ProcessName
            => (RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
                    ? Path.GetFileNameWithoutExtension( this.ImagePath )
                    : Path.GetFileName( this.ImagePath ))
                ?.ToLowerInvariant();

        public ProcessInfo( int processId, string? imageFileName )
        {
            this.ProcessId = processId;
            this.ImagePath = imageFileName;
        }

        public override string ToString() => $"{this.ProcessName}({this.ProcessId})";
    }
}