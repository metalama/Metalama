// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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