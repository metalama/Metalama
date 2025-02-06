// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Records;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

namespace Flashtrace.Messages;

/// <summary>
/// Encapsulates a formatted message without parameter. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
[PublicAPI]
public readonly struct FormattedMessage : IMessage
{
    private readonly string _text;

    [MethodImpl( MethodImplOptions.AggressiveInlining )] // To avoid copying the struct.
    internal FormattedMessage( string text )
    {
        this._text = text;
    }

    void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
    {
        recordBuilder.BeginWriteItem( item, new LogRecordTextOptions( 0 ) );

        if ( !string.IsNullOrEmpty( this._text ) )
        {
            recordBuilder.WriteString( this._text.AsSpan() );
        }

        recordBuilder.EndWriteItem( item );
    }

    /// <inheritdoc/>
    public override string ToString() => this._text ?? "null";
}