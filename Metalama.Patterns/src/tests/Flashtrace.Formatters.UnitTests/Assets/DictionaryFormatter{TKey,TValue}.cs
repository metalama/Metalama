// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Globalization;

namespace Flashtrace.Formatters.UnitTests.Assets
{
    internal class DictionaryFormatter<TKey, TValue> : Formatter<IDictionary<TKey, TValue>>
    {
        public DictionaryFormatter( IFormatterRepository repository ) : base( repository ) { }

        public override void Format( UnsafeStringBuilder stringBuilder, IDictionary<TKey, TValue>? value )
        {
            stringBuilder.Append(
                "{" + string.Join( ",", value!.Select( kvp => string.Format( CultureInfo.InvariantCulture, "{0}:{1}", kvp.Key, kvp.Value ) ) ) + "}" );
        }
    }
}