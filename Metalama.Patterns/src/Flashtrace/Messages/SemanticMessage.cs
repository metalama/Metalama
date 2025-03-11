// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Records;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

namespace Flashtrace.Messages;

/// <summary>
/// Encapsulates a semantic message without parameter. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
[PublicAPI]
public readonly struct SemanticMessage : IMessage
{
    private readonly string _messageName;

    [MethodImpl( MethodImplOptions.AggressiveInlining )] // To avoid copying the struct.
    internal SemanticMessage( string messageName )
    {
        this._messageName = messageName;
    }

    void IMessage.Write( ILogRecordBuilder builder, LogRecordItem item )
    {
        builder.BeginWriteItem( item, new LogRecordTextOptions( 0, this._messageName ) );
        builder.EndWriteItem( item );
    }

    /// <inheritdoc/>
    public override string ToString() => DebugMessageFormatter.Format( this );
}