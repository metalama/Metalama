// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Extensions.DependencyInjection.ServiceLocator;

internal class LazyServiceLocatorDependencyInjectionStrategy : DefaultDependencyInjectionStrategy, ITemplateProvider
{
    public LazyServiceLocatorDependencyInjectionStrategy( DependencyProperties properties ) : base( properties ) { }

    public override IntroduceDependencyResult IntroduceDependency( IAdviser<INamedType> adviser )
    {
        var propertyArgs = new PropertyArgs();

        // Introduce the visible property, something like `IMyService MyService => this._myServiceCache ??= (T) this._serviceProvider.GetService((typeof(T))`.
        var introducePropertyResult = adviser
            .WithTemplateProvider( this )
            .IntroduceProperty(
                this.Properties.Name,
                nameof(GetDependencyTemplate),
                null,
                IntroductionScope.Instance,
                OverrideStrategy.Ignore,
                propertyBuilder =>
                {
                    propertyBuilder.Type = this.Properties.DependencyType;
                    propertyBuilder.Name = this.Properties.Name;
                },
                args: new { args = propertyArgs, T = this.Properties.DependencyType } );

        switch ( introducePropertyResult.Outcome )
        {
            case AdviceOutcome.Ignore:
                return IntroduceDependencyResult.Ignore( introducePropertyResult.Declaration );

            case AdviceOutcome.Error:
                return IntroduceDependencyResult.Error;
        }

        if ( !this.TryAddFields( adviser, introducePropertyResult.Declaration, propertyArgs ) )
        {
            return IntroduceDependencyResult.Error;
        }

        return IntroduceDependencyResult.Success( introducePropertyResult.Declaration );
    }

    public override bool TryImplementDependency( IAdviser<IFieldOrProperty> builder )
    {
        var propertyArgs = new PropertyArgs();

        var overrideResult = builder
            .WithTemplateProvider( this )
            .OverrideAccessors(
                nameof(GetDependencyTemplate),
                builder.Target.Writeability != Writeability.None ? nameof(this.SetDependencyTemplate) : null,
                args: new { args = propertyArgs, T = builder.Target.Type } );

        if ( overrideResult.Outcome == AdviceOutcome.Error )
        {
            return false;
        }

        SuppressNonNullableFieldMustContainValue( builder, builder.Target );

        var typeBuilder = builder.With( builder.Target.DeclaringType );

        if ( !this.TryAddFields( typeBuilder, overrideResult.Declaration, propertyArgs ) )
        {
            return false;
        }

        return true;
    }

    private bool TryAddFields( IAdviser<INamedType> adviser, IProperty property, PropertyArgs propertyArgs )
    {
        // Introduce a field that stores the IServiceProvider.

        var introduceServiceProviderFieldResult = adviser.IntroduceField(
            "_serviceProvider",
            typeof(IServiceProvider),
            whenExists: OverrideStrategy.Ignore );

        if ( introduceServiceProviderFieldResult.Outcome == AdviceOutcome.Error )
        {
            return false;
        }

        propertyArgs.ServiceProviderField = introduceServiceProviderFieldResult.Declaration;

        if ( introduceServiceProviderFieldResult.Outcome != AdviceOutcome.Ignore )
        {
            this.InitializeServiceProvider( adviser, propertyArgs.ServiceProviderField );

            SuppressionHelper.SuppressUnusedWarnings( adviser, propertyArgs.ServiceProviderField );
            SuppressNonNullableFieldMustContainValue( adviser, propertyArgs.ServiceProviderField );
        }

        // Introduce a field that caches the service.
        var introduceCacheFieldResult = adviser.IntroduceField(
            property.Name + "Cache",
            property.Type.ToNullable() );

        if ( introduceCacheFieldResult.Outcome == AdviceOutcome.Error )
        {
            return false;
        }

        propertyArgs.CacheField = introduceCacheFieldResult.Declaration;

        SuppressionHelper.SuppressUnusedWarnings( adviser, propertyArgs.CacheField );

        return true;
    }

    private void InitializeServiceProvider( IAdviser<INamedType> adviser, IField serviceProviderField )
    {
        foreach ( var constructor in adviser.Target.Constructors )
        {
            if ( constructor.InitializerKind != ConstructorInitializerKind.This )
            {
                adviser
                    .WithTemplateProvider( this )
                    .With( constructor )
                    .AddInitializer( nameof(Initializer), args: new { serviceProviderField } );
            }
        }
    }

    public class PropertyArgs
    {
        public IField? CacheField { get; set; }

        public IField? ServiceProviderField { get; set; }
    }

    [Template]
    private static T GetDependencyTemplate<[CompileTime] T>( PropertyArgs args )
    {
        return args.CacheField!.Value ??= (T) args.ServiceProviderField!.Value!.GetService( typeof(T) );
    }

    // ReSharper disable once UnusedParameter.Local
    [Template]
    private void SetDependencyTemplate<[CompileTime] T>( PropertyArgs args )
    {
        throw new NotSupportedException( $"Cannot set '{this.Properties.Name}' because of the dependency aspect." );
    }

    [Template]
    private static void Initializer( IField serviceProviderField )
    {
        serviceProviderField.Value = ServiceProviderProvider.ServiceProvider();
    }
}