// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.UnitTests.Assets;

internal class ThrowingFormatter<T> : Formatter<IEnumerable<T>>
{
    // ReSharper disable once StaticMemberInGenericType
    public static bool Ran;

    public ThrowingFormatter( IFormatterRepository repository ) : base( repository )
    {
        Ran = true;

#pragma warning disable CA2201
        throw new Exception();
#pragma warning restore CA2201
    }

    public override void Format( UnsafeStringBuilder stringBuilder, IEnumerable<T>? value ) => throw new NotSupportedException();
}