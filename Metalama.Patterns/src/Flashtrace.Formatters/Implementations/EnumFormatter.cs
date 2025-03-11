// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.Implementations;

// See http://www.codeproject.com/Articles/278820/Optimized-Enum-ToString
// for a much more complicated enum formatter. But that one always boxes
// (because ToInt32 boxes), which would not be easy to work around.

/// <summary>
/// Efficient formatter for enums.
/// </summary>
internal sealed class EnumFormatter<T> : Formatter<T>
    where T : Enum
{
    public EnumFormatter( IFormatterRepository repository ) : base( repository ) { }

    public override void Format( UnsafeStringBuilder stringBuilder, T? value )
    {
        EnumFormatterCache<T>.Write( stringBuilder, value );
    }
}