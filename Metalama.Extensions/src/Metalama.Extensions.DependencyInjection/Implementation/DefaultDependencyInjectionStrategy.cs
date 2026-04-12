// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    /// then <see cref="GetDependencyPullStrategy"/>, then <see cref="TryPullDependency(IAdviser{INamedType},IFieldOrProperty,IDependencyPullStrategy)"/>.
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

        var pullStrategy = this.GetDependencyPullStrategy( fieldOrProperty );

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
        // Suppress CS0649 ("Field is never assigned to") on the target field because
        // the field will be assigned in generated constructor code.
        adviser.Diagnostics.Suppress( DiagnosticDescriptors.FieldIsNeverAssigned, adviser.Target );

        var pullStrategy = this.GetDependencyPullStrategy( adviser.Target );

        this.TryPullDependency( adviser.WithDeclaringType(), adviser.Target, pullStrategy );

        return true;
    }

    /// <summary>
    /// Gets the constructors that are modified by <see cref="TryPullDependency(IAdviser{INamedType},IFieldOrProperty,IDependencyPullStrategy)"/>.
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
    /// Pulls the dependency from all constructors, i.e. introduce a parameter to these constructors (according to an <see cref="IDependencyPullStrategy"/>), and
    /// assigns its value to the dependency property.
    /// </summary>
    /// <param name="adviser">An <see cref="IAspectBuilder{TAspectTarget}"/> for the target type.</param>
    /// <param name="dependencyFieldOrProperty">The field or property that exposed the dependency.</param>
    /// <param name="dependencyPullStrategy">A pull strategy (typically the one returned by <see cref="GetDependencyPullStrategy"/>).</param>
    protected bool TryPullDependency( IAdviser<INamedType> adviser, IFieldOrProperty dependencyFieldOrProperty, IDependencyPullStrategy dependencyPullStrategy )
    {
        SuppressNonNullableFieldMustContainValue( adviser, dependencyFieldOrProperty );

        var success = true;

        foreach ( var constructor in GetConstructors( adviser.Target ) )
        {
            if ( !this.TryPullDependency( adviser.With( constructor ), dependencyFieldOrProperty, dependencyPullStrategy ) )
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
        IDependencyPullStrategy dependencyPullStrategy )
    {
        var constructor = adviser.Target;

        // Check that the dependency type is at least as accessible as the constructor.
        var constructorEffectiveAccessibility = constructor.GetEffectiveAccessibility();
        var dependencyTypeEffectiveAccessibility = this.Properties.DependencyType.GetEffectiveAccessibility();

        if ( dependencyTypeEffectiveAccessibility < constructorEffectiveAccessibility )
        {
            adviser.Diagnostics.Report(
                DiagnosticDescriptors.DependencyLessAccessibleThanConstructor.WithArguments( (this.Properties.DependencyType, constructor) ),
                constructor );

            return false;
        }

        var pullStrategy = dependencyPullStrategy.CreateParameterPullStrategy();

        // Find a compatible type in the constructor.
        var existingParameter = pullStrategy.GetExistingParameter( constructor );

        // If there is no compatible parameter, create one.
        if ( existingParameter == null )
        {
            var newParameter = pullStrategy.GetNewParameter( constructor );

            // Use the framework's IntroduceParameterPullStrategy with reuseExistingParameterOfCompatibleType
            // so that derived constructors that already have a parameter of the same (or covariant) service type
            // reuse it instead of introducing a duplicate. Two DI service parameters of the same type on a
            // single constructor are never intentional.
            var frameworkPullStrategy = PullStrategy.IntroduceParameterAndPull(
                name: newParameter.Name,
                type: newParameter.Type,
                defaultValue: TypedConstant.Default( newParameter.Type ),
                reuseExistingParameterOfCompatibleType: true );

            existingParameter = adviser.IntroduceParameter(
                    newParameter.Name,
                    newParameter.Type,
                    TypedConstant.Default( newParameter.Type, newParameter.Type.IsNullable == false ),
                    frameworkPullStrategy,
                    newParameter.Attributes )
                .Declaration;
        }

        var assignment = dependencyPullStrategy.GetAssignmentStatement( existingParameter );
        adviser.AddInitializer( assignment );

        return true;
    }

    /// <summary>
    /// Gets an <see cref="IDependencyPullStrategy"/>, i.e. a strategy to pull a dependency field or property from constructors.
    /// </summary>
    /// <param name="introducedFieldOrProperty">The value returned by <see cref="TryIntroduceFieldOrProperty"/>.</param>
    /// <returns>The <see cref="IDependencyPullStrategy"/>.</returns>
    protected virtual IDependencyPullStrategy GetDependencyPullStrategy( IFieldOrProperty introducedFieldOrProperty )
        => new DefaultDependencyPullStrategy( this.Properties, introducedFieldOrProperty );
}