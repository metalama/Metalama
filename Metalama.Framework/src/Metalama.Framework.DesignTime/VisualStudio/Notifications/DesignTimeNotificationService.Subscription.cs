// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Notifications;

namespace Metalama.Framework.DesignTime.VisualStudio.Notifications
{
    internal sealed partial class DesignTimeNotificationService
    {
        private sealed class Subscription : IDisposable
        {
            private readonly DesignTimeNotificationService _parent;
            private readonly IDesignTimeNotificationObserver _observer;
            private readonly string[] _eventTypeNames;
            private int _disposed;

            public Subscription( DesignTimeNotificationService parent, IDesignTimeNotificationObserver observer, string[] eventTypeNames )
            {
                this._parent = parent;
                this._observer = observer;
                this._eventTypeNames = eventTypeNames;
            }

            public void Dispose()
            {
                if ( Interlocked.Exchange( ref this._disposed, 1 ) != 0 )
                {
                    return;
                }

                lock ( this._parent._sync )
                {
                    foreach ( var name in this._eventTypeNames )
                    {
                        if ( this._parent._observersByEventType.TryGetValue( name, out var list ) )
                        {
                            list.Remove( this._observer );
                        }
                    }
                }

                this._parent._logger.Trace?.Log( $"Unsubscribed observer from [{string.Join( ", ", this._eventTypeNames )}]." );
            }
        }
    }
}
