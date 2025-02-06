// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using Metalama.Patterns.Caching.Formatters;

namespace Metalama.Patterns.Caching.TestHelpers
{
    // ReSharper disable once UnusedType.Global
    public class DependencyClass : IFormattable<CacheKeyFormatting>
    {
        private readonly string _key;

        public DependencyClass( string key )
        {
            this._key = key;
        }

        void IFormattable<CacheKeyFormatting>.Format( UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository )
        {
            stringBuilder.Append( this._key );
        }
    }
}