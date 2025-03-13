// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Observability.Configuration;
using System.ComponentModel;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

internal sealed class ClassicDesignTimeObservabilityStrategyImpl : DesignTimeObservabilityStrategy
{
    private readonly IMethod? _baseOnPropertyChangedOverridableMethod;
    private readonly IMethod? _baseOnChildPropertyChangedMethod;
    private readonly IMethod? _baseOnObservablePropertyChangedMethod;

    private IAspectBuilder<INamedType> Builder { get; }

    public ClassicDesignTimeObservabilityStrategyImpl( IAspectBuilder<INamedType> builder )
    {
        this.Builder = builder;

        var target = builder.Target;
        var elements = builder.Target.Compilation.Cache.GetOrAdd( _ => new Assets() );

        (_, this._baseOnPropertyChangedOverridableMethod) =
            ClassicObservabilityStrategyImpl.GetOnPropertyChangedMethods( target );

        this._baseOnChildPropertyChangedMethod = ClassicObservabilityStrategyImpl.GetOnChildPropertyChangedMethod( target );

        this._baseOnObservablePropertyChangedMethod =
            ClassicObservabilityStrategyImpl.GetOnObservablePropertyChangedMethod( target, elements );
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        // Also introduce any methods which might be introduced by the full builder which might reasonably be observed by other code.

        this.IntroduceOnPropertyChangedMethod();
        this.IntroduceOnChildPropertyChangedMethod();
        this.IntroduceOnObservablePropertyChanged();
    }

    private void IntroduceOnPropertyChangedMethod()
    {
        var isOverride = this._baseOnPropertyChangedOverridableMethod != null;

        var template = this._baseOnPropertyChangedOverridableMethod == null
                       || this._baseOnPropertyChangedOverridableMethod.Parameters[0].Type.Equals( SpecialType.String )
            ? nameof(OnPropertyChangedString)
            : nameof(OnPropertyChangedObject);

        this.Builder.Advice.WithTemplateProvider( this )
            .IntroduceMethod(
                this.Builder.Target,
                template,
                IntroductionScope.Instance,
                isOverride ? OverrideStrategy.Override : OverrideStrategy.Ignore,
                b =>
                {
                    if ( isOverride )
                    {
                        b.Name = this._baseOnPropertyChangedOverridableMethod!.Name;
                    }

                    if ( this.Builder.Target.IsSealed )
                    {
                        b.Accessibility = this._baseOnPropertyChangedOverridableMethod?.Accessibility ?? Accessibility.Private;
                    }
                    else
                    {
                        b.Accessibility = this._baseOnPropertyChangedOverridableMethod?.Accessibility ?? Accessibility.Protected;
                        b.IsVirtual = !isOverride;
                    }
                } );
    }

    private void IntroduceOnChildPropertyChangedMethod()
    {
        var isOverride = this._baseOnChildPropertyChangedMethod != null;

        this.Builder.Advice.WithTemplateProvider( this )
            .IntroduceMethod(
                this.Builder.Target,
                nameof(OnChildPropertyChanged),
                IntroductionScope.Instance,
                isOverride ? OverrideStrategy.Override : OverrideStrategy.Ignore,
                b =>
                {
                    if ( isOverride )
                    {
                        b.Name = this._baseOnChildPropertyChangedMethod!.Name;
                    }

                    if ( this.Builder.Target.IsSealed )
                    {
                        b.Accessibility = isOverride ? this._baseOnChildPropertyChangedMethod!.Accessibility : Accessibility.Private;
                    }
                    else
                    {
                        b.Accessibility = isOverride ? this._baseOnChildPropertyChangedMethod!.Accessibility : Accessibility.Protected;
                        b.IsVirtual = !isOverride;
                    }
                } );
    }

    private void IntroduceOnObservablePropertyChanged()
    {
        if ( !this.Builder.Target.Enhancements().GetOptions<ClassicObservabilityStrategyOptions>().EnableOnObservablePropertyChangedMethod
             == true )
        {
            return;
        }

        var isOverride = this._baseOnObservablePropertyChangedMethod != null;

        this.Builder.Advice.WithTemplateProvider( this )
            .IntroduceMethod(
                this.Builder.Target,
                nameof(OnObservablePropertyChanged),
                IntroductionScope.Instance,
                isOverride ? OverrideStrategy.Override : OverrideStrategy.Ignore,
                b =>
                {
                    if ( isOverride )
                    {
                        b.Name = this._baseOnObservablePropertyChangedMethod!.Name;
                    }

                    if ( this.Builder.Target.IsSealed )
                    {
                        b.Accessibility = isOverride ? this._baseOnObservablePropertyChangedMethod!.Accessibility : Accessibility.Private;
                    }
                    else
                    {
                        b.Accessibility = isOverride ? this._baseOnObservablePropertyChangedMethod!.Accessibility : Accessibility.Protected;
                        b.IsVirtual = !isOverride;
                    }
                } );
    }

    [UsedImplicitly]
    [Template( Name = "OnPropertyChanged" )]
    private static void OnPropertyChangedString( string propertyName ) { }

    [UsedImplicitly]
    [Template( Name = "OnPropertyChanged" )]
    private static void OnPropertyChangedObject( PropertyChangedEventArgs args ) { }

    [UsedImplicitly]
    [Template]
    internal static void OnChildPropertyChanged( string parentPropertyPath, string propertyName ) { }

    [UsedImplicitly]
    [Template]
    internal static void OnObservablePropertyChanged( string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue ) { }
}