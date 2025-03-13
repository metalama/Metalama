// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.CompilerServices;

namespace Metalama.Framework.DesignTime.Utilities;

#pragma warning disable SA1402 // File may only contain a single type

// Based on PostSharp.Patterns.Model.WeakEventHandler, but significantly changed.
internal sealed class WeakEvent<TEventArgs> : WeakEventBase<Action<TEventArgs>, TEventArgs>
{
    public void Invoke( TEventArgs eventArgs )
    {
        // Take a snapshot of the targets list.
        var invocationList = this.Targets;

        var needCleanUp = false;

        if ( invocationList == null )
        {
            return;
        }

        foreach ( var obj in invocationList )
        {
            if ( obj == null )
            {
                continue;
            }

            if ( !obj.TryGetTarget( out var target ) )
            {
                needCleanUp = true;
            }
            else
            {
                if ( this.GetHandler( target ) is { } handler )
                {
                    handler.Invoke( eventArgs );
                }
            }
        }

        if ( needCleanUp )
        {
            this.CleanUp();
        }
    }
}

internal sealed class AsyncWeakEvent<TEventArgs> : WeakEventBase<Func<TEventArgs, Task>, TEventArgs>
{
    public async Task InvokeAsync( TEventArgs eventArgs )
    {
        // Take a snapshot of the targets list.
        var invocationList = this.Targets;

        var needCleanUp = false;

        if ( invocationList == null )
        {
            return;
        }

        foreach ( var obj in invocationList )
        {
            if ( obj == null )
            {
                continue;
            }

            if ( !obj.TryGetTarget( out var target ) )
            {
                needCleanUp = true;
            }
            else
            {
                if ( this.GetHandler( target ) is { } handler )
                {
                    await handler.Invoke( eventArgs );
                }
            }
        }

        if ( needCleanUp )
        {
            this.CleanUp();
        }
    }
}

public abstract class WeakEventBase<TDelegate, TEventArgs>
    where TDelegate : MulticastDelegate
{
    private readonly object _lock = new();

    private readonly ConditionalWeakTable<object, TDelegate> _handlers = new();

    protected WeakReference<object>?[]? Targets { get; private set; }

    internal bool HasHandlers()
    {
        var targets = this.Targets;

        if ( targets == null )
        {
            return false;
        }

        foreach ( var t in targets )
        {
            if ( t != null )
            {
                return true;
            }
        }

        return false;
    }

    private void AddHandler( TDelegate handler )
    {
        if ( handler.Target == null )
        {
            throw new ArgumentException( "Delegates with no target are not supported." );
        }

        lock ( this._lock )
        {
            // Take a local copy of the array to keep the shared copy consistent for readers.
            var myTargets = this.Targets;

            int index;

            if ( myTargets == null )
            {
                myTargets = new WeakReference<object>[1];
                index = 0;
            }
            else
            {
                index = -1;

                for ( var i = 0; i < myTargets.Length; i++ )
                {
                    var handlerRef = myTargets[i];

                    if ( handlerRef == null )
                    {
                        index = i;

                        // We continue to loop because we want to keep cleaning the list.
                    }
                    else if ( !handlerRef.TryGetTarget( out _ ) )
                    {
                        myTargets[i] = null;
                    }
                }

                if ( index < 0 )
                {
                    index = myTargets.Length;
                    Array.Resize( ref myTargets, myTargets.Length * 2 );
                }
            }

            this._handlers.Add( handler.Target, handler );

            myTargets[index] = new WeakReference<object>( handler.Target );

            this.Targets = myTargets;
        }
    }

    private void RemoveHandler( TDelegate handler )
    {
        lock ( this._lock )
        {
            // Take a local copy of the array to keep the shared copy consistent for readers.
            var myTargets = this.Targets;

            if ( myTargets == null )
            {
                return;
            }

            for ( var i = myTargets.Length - 1; i >= 0; i-- )
            {
                var targetRef = myTargets[i];

                if ( targetRef != null )
                {
                    if ( !targetRef.TryGetTarget( out var t ) )
                    {
                        myTargets[i] = null;
                    }
                    else if ( t == handler.Target )
                    {
                        this._handlers.Remove( handler.Target );

                        myTargets[i] = null;

                        return;
                    }
                }
            }
        }
    }

    protected void CleanUp()
    {
        lock ( this._lock )
        {
            var myTargets = this.Targets;

            if ( myTargets == null )
            {
                return;
            }

            for ( var i = 0; i < myTargets.Length; i++ )
            {
                var handlerRef = myTargets[i];

                if ( handlerRef != null )
                {
                    if ( !handlerRef.TryGetTarget( out _ ) )
                    {
                        myTargets[i] = null;
                    }
                }
            }
        }
    }

    protected TDelegate? GetHandler( object target )
    {
        this._handlers.TryGetValue( target, out var handler );

        return handler;
    }

    internal Accessors GetAccessors() => new( this );

    public readonly struct Accessors( WeakEventBase<TDelegate, TEventArgs> parent )
    {
        internal void RegisterHandler( TDelegate handler ) => parent.AddHandler( handler );

        internal void UnregisterHandler( TDelegate handler ) => parent.RemoveHandler( handler );
    }
}