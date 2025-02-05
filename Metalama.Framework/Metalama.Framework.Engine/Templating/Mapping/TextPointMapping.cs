// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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