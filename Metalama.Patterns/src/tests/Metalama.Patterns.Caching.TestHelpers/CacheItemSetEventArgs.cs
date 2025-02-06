// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.TestHelpers
{
    [PublicAPI]
    public sealed class CacheItemSetEventArgs : EventArgs
    {
        public string Key { get; }

        public CacheItem Item { get; }

        public string? SourceId { get; }

        internal CacheItemSetEventArgs( string key, CacheItem item, string? sourceId )
        {
            this.Key = key ?? throw new ArgumentNullException( nameof(key) );
            this.Item = item ?? throw new ArgumentNullException( nameof(item) );
            this.SourceId = sourceId;
        }
    }

    public delegate void CacheItemSetEventHandler( object sender, CacheItemSetEventArgs args );
}