// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using FluentAssertions;
using System.Runtime.CompilerServices;
using System.Windows;

#pragma warning disable LAMA5206

// ReSharper disable UnusedMember.Local

namespace Metalama.Patterns.Wpf.UnitTests.Assets.DependencyPropertyNS;

public sealed partial class CallbackTestClass : DependencyObject
{
    public sealed class ThreadContext
    {
        private static readonly ThreadLocal<ThreadContext> _current = new( () => new ThreadContext() );

        public static ThreadContext Current => _current.Value!;

        public void Reset( Func<int, bool>? onValidate = null )
        {
            this.Log.Clear();
            this.OnValidate = onValidate;
        }

        public List<string> Log { get; } = new();

        public Func<int, bool>? OnValidate { get; set; }
    }

    public string Id { get; } = Guid.NewGuid().ToString();

    private static void LogCall( string? suffix = null, [CallerMemberName] string? name = null )
    {
        ThreadContext.Current.Log.Add( suffix == null ? name! : $"{name}|{suffix}" );
    }

    private static void Validate( int value )
    {
        var f = ThreadContext.Current.OnValidate;

        if ( !(f == null || f( value )) )
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    #region OnChanging and OnChanged

    [DependencyProperty]
    public int ImplicitInstanceDependencyProperty { get; set; }

    private void OnImplicitInstanceDependencyPropertyChanged( DependencyProperty dependencyProperty )
    {
        LogCall();
        dependencyProperty.Should().BeSameAs( ImplicitInstanceDependencyPropertyProperty );
    }

    [DependencyProperty]
    public int ImplicitInstanceNoParameters { get; set; }

    private void OnImplicitInstanceNoParametersChanged()
    {
        LogCall();
    }

    [DependencyProperty]
    public int ImplicitInstanceValue { get; set; }

    private void OnImplicitInstanceValueChanged( int value )
    {
        LogCall( $"{value}" );
    }

    [DependencyProperty]
    public int ImplicitInstanceOldValueNewValue { get; set; }

    private void OnImplicitInstanceOldValueNewValueChanged( int oldValue, int newValue )
    {
        LogCall( $"{oldValue}|{newValue}" );
    }

    [DependencyProperty]
    public int ImplicitStaticDependencyProperty { get; set; }

    private static void OnImplicitStaticDependencyPropertyChanged( DependencyProperty dependencyProperty )
    {
        LogCall();
        dependencyProperty.Should().BeSameAs( ImplicitStaticDependencyPropertyProperty );
    }

    [DependencyProperty]
    public int ImplicitStaticDependencyPropertyAndInstance { get; set; }

    private static void OnImplicitStaticDependencyPropertyAndInstanceChanged( DependencyProperty dependencyProperty, CallbackTestClass instance )
    {
        LogCall( instance.Id );
        dependencyProperty.Should().BeSameAs( ImplicitStaticDependencyPropertyAndInstanceProperty );
    }

    [DependencyProperty]
    public int ImplicitStaticInstance { get; set; }

    private static void OnImplicitStaticInstanceChanged( CallbackTestClass instance )
    {
        LogCall( instance.Id );
    }

    [DependencyProperty]
    public int ImplicitStaticNoParameters { get; set; }

    private void OnImplicitStaticNoParametersChanging()
    {
        LogCall();
    }

    private void OnImplicitStaticNoParametersChanged()
    {
        LogCall();
    }

    #endregion

    #region Validate

    [DependencyProperty]
    public int ImplicitInstanceValidateDependencyPropertyAndValue { get; set; }

    private void ValidateImplicitInstanceValidateDependencyPropertyAndValue( DependencyProperty dependencyProperty, int value )
    {
        LogCall( $"{value}" );
        dependencyProperty.Should().BeSameAs( ImplicitInstanceValidateDependencyPropertyAndValueProperty );

        Validate( value );
    }

    private void OnImplicitInstanceValidateDependencyPropertyAndValueChanged()
    {
        LogCall();
    }

    [DependencyProperty]
    public int ImplicitInstanceValidateValue { get; set; }

    private void ValidateImplicitInstanceValidateValue( int value )
    {
        LogCall( $"{value}" );

        Validate( value );
    }

    private void OnImplicitInstanceValidateValueChanged()
    {
        LogCall();
    }

    [DependencyProperty]
    public int ImplicitStaticValidateDependencyPropertyAndInstanceAndValue { get; set; }

    private static void ValidateImplicitStaticValidateDependencyPropertyAndInstanceAndValue(
        DependencyProperty dependencyProperty,
        CallbackTestClass instance,
        int value )
    {
        LogCall( $"{instance.Id}|{value}" );
        dependencyProperty.Should().BeSameAs( ImplicitStaticValidateDependencyPropertyAndInstanceAndValueProperty );

        Validate( value );
    }

    private void OnImplicitStaticValidateDependencyPropertyAndInstanceAndValueChanged()
    {
        LogCall();
    }

    [DependencyProperty]
    public int ImplicitStaticValidateDependencyPropertyAndValue { get; set; }

    private static void ValidateImplicitStaticValidateDependencyPropertyAndValue( DependencyProperty dependencyProperty, int value )
    {
        LogCall( $"{value}" );
        dependencyProperty.Should().BeSameAs( ImplicitStaticValidateDependencyPropertyAndValueProperty );

        Validate( value );
    }

    private void OnImplicitStaticValidateDependencyPropertyAndValueChanged()
    {
        LogCall();
    }

    [DependencyProperty]
    public int ImplicitStaticValidateValue { get; set; }

    private void ValidateImplicitStaticValidateValue( int value )
    {
        LogCall( $"{value}" );

        Validate( value );
    }

    private void OnImplicitStaticValidateValueChanged()
    {
        LogCall();
    }

    #endregion
}