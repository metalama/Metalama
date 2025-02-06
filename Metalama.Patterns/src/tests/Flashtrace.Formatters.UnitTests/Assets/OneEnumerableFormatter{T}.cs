// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.UnitTests.Assets;

internal class OneEnumerableFormatter<T> : OneFormatter<IEnumerable<T>>
{
    public OneEnumerableFormatter( IFormatterRepository repository ) : base( repository ) { }
}