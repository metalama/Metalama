// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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