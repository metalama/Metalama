// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.WeakEvent;

#pragma warning disable CS0618 // Type or member is obsolete

internal class WeakEventAttribute : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        var invokeMethod = builder.Target.Type.Methods.OfName( "Invoke" ).Single();

        var argsTupleType = 
            TypeFactory.GetType( $"System.ValueTuple`{invokeMethod.Parameters.Count}" )
            .WithTypeArguments( invokeMethod.Parameters.Select( p => p.Type).ToArray() );

        var eventArgsType = invokeMethod.Parameters[1].Type;

        var invokerType =
            ((INamedType) TypeFactory.GetType( typeof( WeakEventInvokerForEventHandler<> ) )).WithTypeArguments( eventArgsType );

        var containerType =
            ((INamedType) TypeFactory.GetType( typeof( WeakEventContainer<,,> ) )).WithTypeArguments(
                builder.Target.Type,
                argsTupleType,
                invokerType );

        var containerField = builder.With( builder.Target.DeclaringType ).IntroduceField(
            $"weakEventContainerFor{builder.Target.Name}",
            containerType,
            IntroductionScope.Instance,
            OverrideStrategy.Fail,
            b =>
            {

            } ).Declaration;

        builder.OverrideAccessors(
            nameof( OverrideAdd ),
            nameof( OverrideRemove ),
            nameof( OverrideInvoke ),
            new { container = containerField } );
    }

    [Template]
    public void OverrideAdd( [CompileTime] IField container, dynamic value )
    {
        container.Value.AddHandler( value );
    }

    [Template]
    public void OverrideRemove( [CompileTime] IField container, dynamic value )
    {
        container.Value.RemoveHandler( value );
    }

    [Template]
    public void OverrideInvoke( [CompileTime] IField container, dynamic? handler )
    {
        container.Value.Invoke( (meta.Target.Parameters[1].Value, meta.Target.Parameters[2].Value) );
    }
}

public abstract class WeakEventInvoker<TDelegate, TArgs>
{
    public abstract void Invoke( TDelegate handler, TArgs args );
}

public class WeakEventInvokerForEventHandler<TEventArgs> : WeakEventInvoker<EventHandler<TEventArgs>, (object, TEventArgs)>
{
    public override void Invoke( EventHandler<TEventArgs> handler, (object, TEventArgs) args )
    {
        handler.Invoke( args.Item1, args.Item2 );
    }
}

public class WeakEventContainer<TDelegate, TArgs, TInvoker>
    where TDelegate : Delegate
    where TInvoker : WeakEventInvoker<TDelegate, TArgs>, new()
{
    // This is a demo class, do not use, it will not work.
    private static TInvoker _invoker = new TInvoker();

    private List<WeakReference<TDelegate>> _delegates = new ();

    public void AddHandler(TDelegate handler)
    {
        this._delegates.Add( new WeakReference<TDelegate>( handler ) );
    }

    public void RemoveHandler(TDelegate handler)
    {
        for(var i = this._delegates.Count - 1; i >= 0; i++)
        {
            if (!this._delegates[i].TryGetTarget(out var existing)
                || existing == handler )
            {
                this._delegates.RemoveAt( i );
            }
        }
    }

    public void Invoke(TArgs args)
    {
        for(var i = 0; i < this._delegates.Count; i++)
        {
            if ( this._delegates[i].TryGetTarget( out var @delegate ) )
            {
                _invoker.Invoke( @delegate, args );
            }
            else
            {
                this._delegates.RemoveAt( i );
            }
        }
    }
}

// <target>
internal class TargetCode
{
    private List<EventHandler<EventArgs>> _delegates = new List<EventHandler<EventArgs>>();

    [WeakEvent]
    public event EventHandler<EventArgs> EventField;

    [WeakEvent]
    public event EventHandler<EventArgs> Event
    {
        add
        {
            this._delegates.Add( value );
        }

        remove
        {
            this._delegates.Remove( value );
        }
    }

    public void OnEventField()
    {
        this.EventField.Invoke( this, EventArgs.Empty );
    }

    public void OnEvent()
    {
        foreach(var @delegate in this._delegates)
        {
            @delegate.Invoke( this, EventArgs.Empty );
        }
    }
}
