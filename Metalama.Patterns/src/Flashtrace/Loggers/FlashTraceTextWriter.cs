// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Messages;
using JetBrains.Annotations;
using System.Text;

namespace Flashtrace.Loggers;

[PublicAPI]
public class FlashTraceTextWriter : TextWriter
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly FlashtraceLevelSource _source;

    public FlashTraceTextWriter( FlashtraceLevelSource source )
    {
        this._source = source;
    }

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write( char value )
    {
        this._stringBuilder.Append( value );
    }

    public override void Write( string? value )
    {
        this._stringBuilder.Append( value );
    }

    public override void WriteLine()
    {
        this._source.IfEnabled?.Write( FormattedMessageBuilder.Formatted( this._stringBuilder.ToString() ) );
        this._stringBuilder.Clear();
    }
}