// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using FluentAssertions;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Patterns.Observability.UnitTests;

// TODO: When the options/configration API is available, refactor all unit tests to support multiple configuration permutations.
// That is - a shared project and multiple 'concrete' projects. Initially support enabled/disabled OnObservablePropertyChangedMethod
// feature.

public abstract class InpcTestsBase : IDisposable
{
    // NB: For now assumes single-thread use.

    public sealed class Subscription : IDisposable, IEquatable<Subscription>
    {
        private int _disposed;

        internal Subscription( string origin, INotifyPropertyChanged source )
        {
            this.Origin = origin;
            this.Source = source;
        }

        public string Origin { get; }

        public INotifyPropertyChanged Source { get; }

        internal bool IsDisposed => this._disposed == 1;

        private PropertyChangedEventHandler? _handler;

        internal void SetHandler( PropertyChangedEventHandler handler )
        {
            if ( this._disposed == 1 )
            {
                throw new ObjectDisposedException( "Subscription:" + this.Origin );
            }

            if ( this._handler != null )
            {
                throw new InvalidOperationException( "Handler already set." );
            }

            this._handler = handler;
            this.Source.PropertyChanged += handler;
        }

        public void Dispose()
        {
            if ( this._disposed == 0 )
            {
                this._disposed = 1;
                var handler = this._handler;
                this._handler = null;
                this.Source.PropertyChanged -= handler;
            }
        }

        public override string ToString() => this.Origin;

        public bool Equals( Subscription? other )
            => ReferenceEquals( this, other ) || ReferenceEquals( this, AnySubscription ) || ReferenceEquals( other, AnySubscription );

        public override bool Equals( object? obj ) => this.Equals( obj as Subscription );

        public override int GetHashCode() => 0;
    }

    protected static readonly Subscription AnySubscription = new( "<any>", null! );

    // ReSharper disable NotAccessedPositionalProperty.Global
    protected record struct Event( Subscription Subscription, string PropertyName )
    {
        // ReSharper restore NotAccessedPositionalProperty.Global

        public static implicit operator Event( string propertyName ) => new( AnySubscription, propertyName );

        public static implicit operator Event( (Subscription Subscription, string PropertyName) tuple ) => new( tuple.Subscription, tuple.PropertyName );
    }

    protected Event[] EventsFrom( Action action )
    {
        var list = this.Events;
        list.Should().BeEmpty();

        try
        {
            action();

            return list.ToArray();
        }
        finally
        {
            this.Events.Clear();
        }
    }

    protected List<Event> Events { get; } = new();

    private readonly Dictionary<string, Subscription> _subscriptions = new();

    protected Subscription SubscribeTo<TInpc>( TInpc source, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1 )
        where TInpc : class, INotifyPropertyChanged
    {
        var type = source.GetType();

        var key = $"{type.Name} ({callerMemberName}#{callerLineNumber})";

        var idx = 2;

        while ( this._subscriptions.ContainsKey( key ) )
        {
            key = $"{typeof(TInpc).Name}#{idx} ({callerMemberName}#{callerLineNumber})";
            ++idx;
        }

        var eventsList = this.Events;
        var sub = new Subscription( key, source );

        void Handler( object? sender, PropertyChangedEventArgs args )
        {
            if ( sub.IsDisposed )
            {
                throw new InvalidOperationException( "The handler was invoked after unsubscribing." );
            }

            sender.Should().NotBeNull();
            args.Should().NotBeNull();
            sender.Should().BeSameAs( source );
            args.PropertyName.Should().NotBeNullOrEmpty();

            type.GetProperty( args.PropertyName!, BindingFlags.Instance | BindingFlags.Public )
                .Should()
                .NotBeNull( "because the notified property name should be a public property of the notifying type." );

            eventsList.Add( new Event( sub, args.PropertyName! ) );
        }

        sub.SetHandler( Handler );

        this._subscriptions.Add( key, sub );

        return sub;
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual void OnDispose() { }

    public void Dispose()
    {
        this.OnDispose();

        foreach ( var sub in this._subscriptions.Values )
        {
            sub.Dispose();
        }

        this._subscriptions.Clear();
    }
}