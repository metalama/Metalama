// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.Text;
using System.IO;

namespace Metalama.Framework.Engine.Templating.Mapping
{
    /// <summary>
    /// Represent a position in a text file.
    /// </summary>
    /// <param name="Character">Position of the character counted from the beginning of the file.</param>
    /// <param name="LinePosition">Line and column.</param>
    internal sealed record TextPoint( int Character, LinePosition LinePosition )
    {
        public void Write( BinaryWriter writer )
        {
            writer.Write( this.Character );
            writer.Write( this.LinePosition.Line );
            writer.Write( this.LinePosition.Character );
        }

        public static TextPoint Read( BinaryReader reader ) => new( reader.ReadInt32(), new LinePosition( reader.ReadInt32(), reader.ReadInt32() ) );
    }
}