// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Records;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

namespace Flashtrace.Messages;

/// <summary>
/// Encapsulates a semantic message with an arbitrary number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
[PublicAPI]
public readonly struct SemanticMessageArray : IMessage
{
    private readonly string _messageName;
    private readonly IReadOnlyList<(string Name, object? Value)> _parameters;

    [MethodImpl( MethodImplOptions.AggressiveInlining )] // To avoid copying the struct.
    internal SemanticMessageArray( string messageName, IReadOnlyList<(string Name, object? Value)> parameters )
    {
        this._messageName = messageName;
        this._parameters = parameters;
    }

    void IMessage.Write( ILogRecordBuilder builder, LogRecordItem item )
    {
        builder.BeginWriteItem( item, new LogRecordTextOptions( this._parameters.Count, this._messageName ) );

        for ( var i = 0; i < this._parameters.Count; i++ )
        {
            var (name, value) = this._parameters[i];

            builder.WriteParameter( i, name.AsSpan(), value, LogParameterOptions.SemanticParameter );
        }

        builder.EndWriteItem( item );
    }

    /// <inheritdoc/>
    public override string ToString() => DebugMessageFormatter.Format( this );
}