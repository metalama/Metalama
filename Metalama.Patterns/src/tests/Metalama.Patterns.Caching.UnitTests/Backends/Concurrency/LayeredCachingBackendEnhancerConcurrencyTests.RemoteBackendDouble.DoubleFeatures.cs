// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class LayeredCachingBackendEnhancerConcurrencyTests
    {
        private sealed partial class RemoteBackendDouble
        {
            /// <summary>
            /// The features of a <see cref="RemoteBackendDouble"/>.
            /// </summary>
            /// <remarks>
            /// The enhancer subscribes to the events of its second layer only when <see cref="CachingBackendFeatures.Events"/>
            /// is <see langword="true"/>. It writes a tombstone into its first layer on a removal only when
            /// <see cref="CachingBackendFeatures.Blocking"/> is <see langword="false"/>.
            /// </remarks>
            private sealed class DoubleFeatures : CachingBackendFeatures
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="DoubleFeatures"/> class.
                /// </summary>
                /// <param name="blocking">The value of <see cref="Blocking"/>.</param>
                /// <param name="events">The value of <see cref="Events"/>.</param>
                public DoubleFeatures( bool blocking, bool events )
                {
                    this.Blocking = blocking;
                    this.Events = events;
                }

                /// <inheritdoc />
                public override bool Blocking { get; }

                /// <inheritdoc />
                public override bool Events { get; }

                /// <inheritdoc />
                public override bool Dependencies => true;

                /// <inheritdoc />
                public override bool ContainsDependency => true;
            }
        }
    }
}
