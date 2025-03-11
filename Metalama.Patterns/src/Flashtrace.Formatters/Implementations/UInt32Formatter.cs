// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// A formatter for <see cref="uint"/> values.
/// </summary>
internal sealed class UInt32Formatter : Formatter<uint>
{
    public UInt32Formatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, uint value )
    {
        stringBuilder.Append( value );
    }
}