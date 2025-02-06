// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.ComponentModel;

namespace Metalama.Patterns.Observability.Implementation;

[CompileTime]
internal sealed class Assets
{
    public Assets()
    {
        this.INotifyPropertyChanged = (INamedType) TypeFactory.GetType( typeof(INotifyPropertyChanged) );
        this.PropertyChangedEventOfINotifyPropertyChanged = this.INotifyPropertyChanged.Events.First();
        this.NullableINotifyPropertyChanged = this.INotifyPropertyChanged.ToNullable();
        this.PropertyChangedEventHandler = (INamedType) TypeFactory.GetType( typeof(PropertyChangedEventHandler) );
        this.NullablePropertyChangedEventHandler = this.PropertyChangedEventHandler.ToNullable();
        this.NotObservableAttribute = (INamedType) TypeFactory.GetType( typeof(NotObservableAttribute) );
        this.EqualityComparerOfT = (INamedType) TypeFactory.GetType( typeof(EqualityComparer<>) );
        this.InvokedForAttribute = (INamedType) TypeFactory.GetType( typeof(ObservedExpressionsAttribute) );
    }

    public INamedType InvokedForAttribute { get; }

    // ReSharper disable once InconsistentNaming
    public INamedType INotifyPropertyChanged { get; }

    public INamedType NullableINotifyPropertyChanged { get; }

    public IEvent PropertyChangedEventOfINotifyPropertyChanged { get; }

    private INamedType PropertyChangedEventHandler { get; }

    public INamedType NullablePropertyChangedEventHandler { get; }

    public INamedType NotObservableAttribute { get; }

    private INamedType EqualityComparerOfT { get; }

    /// <summary>
    /// Gets the <see cref="IProperty"/> for property <c>EqualityComparer<paramref name="type"/>>.Default</c>.
    /// </summary>
    public IProperty GetDefaultEqualityComparerForType( IType type )
        => this.EqualityComparerOfT.WithTypeArguments( type ).Properties.OfName( "Default" ).Single();
}