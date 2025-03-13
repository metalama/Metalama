// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.IO;

namespace Metalama.Framework.Engine.Templating.Mapping
{
    /// <summary>
    /// Represents a mapping between a source and a target <see cref="TextPoint"/>.
    /// </summary>
    internal sealed record TextPointMapping( TextPoint Source, TextPoint Target )
    {
        public void Write( BinaryWriter writer )
        {
            this.Source.Write( writer );
            this.Target.Write( writer );
        }

        public static TextPointMapping Read( BinaryReader reader ) => new( TextPoint.Read( reader ), TextPoint.Read( reader ) );
    }
}