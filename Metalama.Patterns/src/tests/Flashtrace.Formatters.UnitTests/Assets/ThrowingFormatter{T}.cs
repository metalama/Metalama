// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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