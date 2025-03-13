// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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