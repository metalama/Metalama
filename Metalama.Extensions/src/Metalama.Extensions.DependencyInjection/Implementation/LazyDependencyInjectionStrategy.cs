// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// A dependency implementation strategy that resolves the dependencies the first time they are used and pull a <see cref="Func{TResult}"/>
/// from the constructor.
/// </summary>
public partial class LazyDependencyInjectionStrategy : DefaultDependencyInjectionStrategy, ITemplateProvider
{
    public LazyDependencyInjectionStrategy( DependencyProperties properties ) : base( properties ) { }

    public override IntroduceDependencyResult IntroduceDependency( IAdviser<INamedType> adviser )
    {
        var propertyArgs = new TemplateArgs();

        // Introduce the visible property, something like `IMyService MyService => this._myServiceCache ??= this._myServiceFunc`.
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
                args: new { args = propertyArgs } );

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

    private bool TryAddFields( IAdviser<INamedType> adviser, IProperty property, TemplateArgs propertyArgs )
    {
        // Introduce a field that stores the Func<>
        var dependencyFieldType = ((INamedType) TypeFactory.GetType( typeof(Func<>) )).WithTypeArguments( property.Type );

        var introduceFuncFieldResult = adviser.IntroduceField(
            property.Name + "Func",
            dependencyFieldType );

        if ( introduceFuncFieldResult.Outcome == AdviceOutcome.Error )
        {
            return false;
        }

        SuppressionHelper.SuppressUnusedWarnings( adviser, introduceFuncFieldResult.Declaration );

        propertyArgs.DependencyField = introduceFuncFieldResult.Declaration;

        // Introduce a field that caches
        var introduceCacheFieldResult = adviser.IntroduceField(
            property.Name + "Cache",
            property.Type.ToNullable() );

        if ( introduceCacheFieldResult.Outcome == AdviceOutcome.Error )
        {
            return false;
        }

        SuppressionHelper.SuppressUnusedWarnings( adviser, introduceCacheFieldResult.Declaration );

        propertyArgs.CacheField = introduceCacheFieldResult.Declaration;

        var pullStrategy = new PullStrategy( this.Properties, property, introduceFuncFieldResult.Declaration );

        return this.TryPullDependency( adviser, propertyArgs.DependencyField, pullStrategy );
    }

    public override bool TryImplementDependency( IAdviser<IFieldOrProperty> adviser )
    {
        var templateArgs = new TemplateArgs();

        var overrideResult = adviser
            .WithTemplateProvider( this )
            .OverrideAccessors(
                nameof(GetDependencyTemplate),
                adviser.Target.Writeability != Writeability.None ? nameof(this.SetDependencyTemplate) : null,
                args: new { args = templateArgs } );

        if ( overrideResult.Outcome == AdviceOutcome.Error )
        {
            return false;
        }

        SuppressNonNullableFieldMustContainValue( adviser, adviser.Target );

        return this.TryAddFields( adviser.With( adviser.Target.DeclaringType ), overrideResult.Declaration, templateArgs );
    }

    public class TemplateArgs
    {
        public IField? CacheField { get; set; }

        public IField? DependencyField { get; set; }
    }

    [Template]
    private static dynamic? GetDependencyTemplate( TemplateArgs args ) => args.CacheField!.Value ??= args.DependencyField!.Value!.Invoke();

    // ReSharper disable once UnusedParameter.Local
    [Template]
    private void SetDependencyTemplate( TemplateArgs args )
        => throw new NotSupportedException( $"Cannot set '{this.Properties.Name}' because of the dependency aspect." );
}