// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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