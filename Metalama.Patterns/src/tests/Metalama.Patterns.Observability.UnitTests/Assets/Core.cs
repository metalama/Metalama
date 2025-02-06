// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.ComponentModel;

namespace Metalama.Patterns.Observability.UnitTests.Assets.Core;

/// <summary>
/// An [NPC]-enhanced base class with three int auto-properties.
/// </summary>
[Observable]
public partial class Simple
{
    /// <summary>
    /// Auto
    /// </summary>
    public int S1 { get; set; }

    /// <summary>
    /// Auto
    /// </summary>
    public int S2 { get; set; }

    /// <summary>
    /// Auto
    /// </summary>
    public int S3 { get; set; }
}

[Observable]
public partial class SimpleWithInpcProperties
{
    /// <summary>
    /// Auto
    /// </summary>
    public int A1 { get; set; }

    /// <summary>
    /// Ref to R1.S1. Will throw if R1 is null.
    /// </summary>
    public int A2 => this.R1!.S1;

    /// <summary>
    /// Property type implements <see cref="INotifyPropertyChanged"/>. <c>R1.S1</c> is referenced by <see cref="A2"/>.
    /// </summary>
    public Simple? R1 { get; set; }

    /// <summary>
    /// Property type implements <see cref="INotifyPropertyChanged"/>, is not referenced in the current class.
    /// </summary>
    public Simple? R2 { get; set; }
}

public class ExistingInpcImplWithValidOpcMethod : INotifyPropertyChanged
{
    private int _ex1;

    public int EX1
    {
        get => this._ex1;
        set
        {
            if ( value != this._ex1 )
            {
                this._ex1 = value;
                this.OnPropertyChanged( nameof(this.EX1) );
            }
        }
    }

    private Simple? _ex2 = new();

    public Simple? EX2
    {
        get => this._ex2;

        set
        {
            if ( this._ex2 != value )
            {
                this._ex2 = value;
                this.OnPropertyChanged( nameof(this.EX2) );
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged( string propertyName )
    {
        this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
    }
}

public abstract class ExistingAbstractInpcImplWithValidOPCMethod : INotifyPropertyChanged
{
    private int _ex1;

    public int EX1
    {
        get => this._ex1;
        set
        {
            if ( value != this._ex1 )
            {
                this._ex1 = value;
                this.OnPropertyChanged( nameof(this.EX1) );
            }
        }
    }

    private Simple? _ex2 = new();

    public Simple? EX2
    {
        get => this._ex2;

        set
        {
            if ( this._ex2 != value )
            {
                this._ex2 = value;
                this.OnPropertyChanged( nameof(this.EX2) );
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged( string propertyName )
    {
        this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
    }
}

[Observable]
public partial class FieldBackedInpcProperty
{
    public FieldBackedInpcProperty()
    {
        this._value = new Simple();
    }

    public void SetValue( Simple v ) => this._value = v;

#pragma warning disable IDE0032 // Use auto property
    private Simple _value;
#pragma warning restore IDE0032 // Use auto property

#pragma warning disable IDE0032 // Use auto property

    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    public Simple P1 => this._value;
#pragma warning restore IDE0032 // Use auto property

    public int P2 => this._value.S1;
}

[Observable]
public partial class FieldBackedIntProperty
{
    public void SetValue( int v ) => this._value = v;

#pragma warning disable IDE0032 // Use auto property
    private int _value;
#pragma warning restore IDE0032 // Use auto property

#pragma warning disable IDE0032 // Use auto property

    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    // ReSharper disable once MemberCanBePrivate.Global
    public int P1 => this._value;
#pragma warning restore IDE0032 // Use auto property

    public int P2 => this.P1;
}

[Observable]
public partial class PrivateIntProperty
{
    public void SetP1( int v ) => this.P1 = v;

    private int P1 { get; set; }

    public int P2 => this.P1;
}

[Observable]
public partial class PrivateInpcProperty
{
    public PrivateInpcProperty()
    {
        this.P1 = new Simple();
    }

    public void SetP1( Simple v ) => this.P1 = v;

    private Simple P1 { get; set; }

    public int P2 => this.P1.S1;
}

[ExcludeAspect( typeof(ObservableAttribute) )]
public class PrivateInpcPropertyWithExposedOnChildPropertyChanged : PrivateInpcProperty
{
    protected override void OnChildPropertyChanged( string parentPropertyPath, string propertyName )
    {
        this.ExposeOnChildPropertyChanged?.Invoke( parentPropertyPath, propertyName );
        base.OnChildPropertyChanged( parentPropertyPath, propertyName );
    }

    public event Action<string, string>? ExposeOnChildPropertyChanged;
}

[Observable]
public partial class ReferenceToNonInpcPropertyOfTargetType
{
    public int P1 { get; set; }

    public int P2 => this.P1;
}