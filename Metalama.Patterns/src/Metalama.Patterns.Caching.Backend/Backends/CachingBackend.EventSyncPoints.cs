// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Common.Testing.Hooks;

namespace Metalama.Patterns.Caching.Backends
{
    public abstract partial class CachingBackend
    {
        /// <summary>
        /// The names of the synchronization points of the event accessors of <see cref="CachingBackend"/>. A test enables a synchronization
        /// point by its name through an <see cref="ITestSynchronizationProvider"/>.
        /// </summary>
        internal static class EventSyncPoints
        {
            /// <summary>
            /// Reached by the <c>add</c> accessor of an event after it has read the current delegate and before it
            /// attempts to replace it.
            /// </summary>
            public const string AddHandlerDelegateRead = "CachingBackend.AddHandler:DelegateRead";

            /// <summary>
            /// Reached by the <c>remove</c> accessor of an event after it has read the current delegate and before it
            /// attempts to replace it.
            /// </summary>
            public const string RemoveHandlerDelegateRead = "CachingBackend.RemoveHandler:DelegateRead";
        }
    }
}
