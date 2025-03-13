// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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