// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.UnitTests.Assets;

internal class NoConstructorFormatter<T> : Formatter<IEnumerable<T>>
{
    private NoConstructorFormatter() : base( null! ) { }

    public override void Format( UnsafeStringBuilder stringBuilder, IEnumerable<T>? value )
    {
        throw new NotSupportedException();
    }
}