// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// The default implementation of the <see cref="IDependencyInjectionFramework.IntroduceDependency"/> interface method. It is designed
/// to be easily extended and overwritten.
/// </summary>
[CompileTime]
public class DefaultDependencyInjectionStrategy
{
    /// <summary>
    /// Gets the <see cref="DependencyProperties"/> for which the current object was created.
    /// </summary>
    protected DependencyProperties Properties { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDependencyInjectionStrategy"/> class.
    /// </summary>
    public DefaultDependencyInjectionStrategy( DependencyProperties properties )
    {
        this.Properties = properties;
    }

    /// <summary>
    /// Introduces the field or property into the target class.
    /// </summary>
    /// <param name="adviser">An <see cref="IAspectBuilder{TAspectTarget}"/> for the target class.</param>
    /// <param name="introducedFieldOrProperty">At output, the created field or property.</param>
    /// <param name="outcome"></param>
    private bool TryIntroduceFieldOrProperty(
        IAdviser<INamedType> adviser,
        [NotNullWhen( true )] out IFieldOrProperty? introducedFieldOrProperty,
        out AdviceOutcome outcome )
    {
        var adviceResult =
            this.Properties.Kind switch
            {
                DeclarationKind.Field =>
                    (IIntroductionAdviceResult<IFieldOrProperty>) adviser.IntroduceField(
                        this.Properties.Name,
                        this.Properties.DependencyType,
                        IntroductionScope.Instance,
                        OverrideStrategy.Ignore ),

                DeclarationKind.Property =>
                    adviser.IntroduceAutomaticProperty(
                        this.Properties.Name,
                        this.Properties.DependencyType,
                        IntroductionScope.Instance,
                        OverrideStrategy.Ignore ),
                _ => throw new InvalidOperationException()
            };

        outcome = adviceResult.Outcome;

        if ( adviceResult.Outcome is AdviceOutcome.Default or AdviceOutcome.Ignore )
        {
            introducedFieldOrProperty = adviceResult.Declaration;

            return true;
        }
        else
        {
            introducedFieldOrProperty = adviceResult.Declaration;

            return false;
        }
    }

    /// <summary>
    /// The entry point of the <see cref="DefaultDependencyInjectionStrategy"/>. Orchestrates all steps: first calls <see cref="TryIntroduceFieldOrProperty"/>,
    /// then <see cref="GetPullStrategy"/>, then <see cref="TryPullDependency(Metalama.Framework.Advising.IAdviser{Metalama.Framework.Code.INamedType},Metalama.Framework.Code.IFieldOrProperty,Metalama.Extensions.DependencyInjection.Implementation.IPullStrategy)"/>.
    /// </summary>
    public virtual IntroduceDependencyResult IntroduceDependency( IAdviser<INamedType> adviser )
    {
        if ( !this.TryIntroduceFieldOrProperty( adviser, out var fieldOrProperty, out var outcome ) )
        {
            return IntroduceDependencyResult.Error;
        }
        else if ( outcome == AdviceOutcome.Ignore )
        {
            return IntroduceDependencyResult.Ignore( fieldOrProperty );
        }

        SuppressionHelper.SuppressUnusedWarnings( adviser, fieldOrProperty );

        var pullStrategy = this.GetPullStrategy( fieldOrProperty );

        if ( !this.TryPullDependency( adviser, fieldOrProperty, pullStrategy ) )
        {
            return IntroduceDependencyResult.Error;
        }
        else
        {
            return IntroduceDependencyResult.Success( fieldOrProperty );
        }
    }

    public virtual bool TryImplementDependency( IAdviser<IFieldOrProperty> adviser )
    {
        var pullStrategy = this.GetPullStrategy( adviser.Target );

        this.TryPullDependency( adviser.With( adviser.Target.DeclaringType ), adviser.Target, pullStrategy );

        return true;
    }

    /// <summary>
    /// Gets the constructors that are modified by <see cref="TryPullDependency(Metalama.Framework.Advising.IAdviser{Metalama.Framework.Code.INamedType},Metalama.Framework.Code.IFieldOrProperty,Metalama.Extensions.DependencyInjection.Implementation.IPullStrategy)"/>.
    /// </summary>
    /// <param name="type">The type in which the dependency is being injected.</param>
    private static IEnumerable<IConstructor> GetConstructors( INamedType type )
        => type.Constructors.Where( c => c.InitializerKind != ConstructorInitializerKind.This && !c.IsRecordCopyConstructor() );

    /// <summary>
    /// Suppresses the warning CS8618 ("Non-nullable variable must contain a non-null value when exiting constructor.") for a member that is being introduced,
    /// if necessary. This is useful for design-time diagnostics.
    /// </summary>
    protected static void SuppressNonNullableFieldMustContainValue( IAdviser adviser, IFieldOrProperty introducedMember )
    {
        SuppressionHelper.SuppressNonNullableFieldMustContainValue( adviser, introducedMember, GetConstructors( introducedMember.DeclaringType ) );
    }

    /// <summary>
    /// Pulls the dependency from all constructors, i.e. introduce a parameter to these constructors (according to an <see cref="IPullStrategy"/>), and
    /// assigns its value to the dependency property.
    /// </summary>
    /// <param name="adviser">An <see cref="IAspectBuilder{TAspectTarget}"/> for the target type.</param>
    /// <param name="dependencyFieldOrProperty">The field or property that exposed the dependency.</param>
    /// <param name="pullStrategy">A pull strategy (typically the one returned by <see cref="GetPullStrategy"/>).</param>
    protected bool TryPullDependency( IAdviser<INamedType> adviser, IFieldOrProperty dependencyFieldOrProperty, IPullStrategy pullStrategy )
    {
        SuppressNonNullableFieldMustContainValue( adviser, dependencyFieldOrProperty );

        var success = true;

        foreach ( var constructor in GetConstructors( adviser.Target ) )
        {
            if ( !this.TryPullDependency( adviser.With( constructor ), dependencyFieldOrProperty, pullStrategy ) )
            {
                success = false;
            }
        }

        return success;
    }

    /// <summary>
    /// Pulls the dependency from a given constructor.
    /// </summary>
    protected virtual bool TryPullDependency(
        IAdviser<IConstructor> adviser,
        IFieldOrProperty dependencyFieldOrProperty,
        IPullStrategy pullStrategy )
    {
        var constructor = adviser.Target;

        // Find a compatible type in the constructor.
        var existingParameter = pullStrategy.GetExistingParameter( constructor );

        // If there is no compatible parameter, create one.
        if ( existingParameter == null )
        {
            var newParameter = pullStrategy.GetNewParameter( constructor );

            existingParameter = adviser.IntroduceParameter(
                    newParameter.Name,
                    newParameter.Type,
                    TypedConstant.Default( newParameter.Type ),
                    pullStrategy.PullParameter,
                    newParameter.Attributes )
                .Declaration;
        }

        var assignment = pullStrategy.GetAssignmentStatement( existingParameter );
        adviser.AddInitializer( assignment );

        return true;
    }

    /// <summary>
    /// Gets an <see cref="IPullStrategy"/>, i.e. a strategy to pull a dependency field or property from constructors.
    /// </summary>
    /// <param name="introducedFieldOrProperty">The value returned by <see cref="TryIntroduceFieldOrProperty"/>.</param>
    /// <returns>The <see cref="IPullStrategy"/>.</returns>
    protected virtual IPullStrategy GetPullStrategy( IFieldOrProperty introducedFieldOrProperty )
        => new DefaultPullStrategy( this.Properties, introducedFieldOrProperty );
}