// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Project;
using Metalama.Patterns.Wpf.Configuration;
using Metalama.Patterns.Wpf.Implementation.DependencyPropertyNamingConvention;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Windows;
using MetalamaAccessibility = Metalama.Framework.Code.Accessibility;

namespace Metalama.Patterns.Wpf.Implementation;

[CompileTime]
internal sealed partial class DependencyPropertyAspectBuilder
{
    internal const string RegistrationFieldCategory = "registration field";
    internal const string PropertyChangedMethodCategory = "property-changed method";
    internal const string ValidateMethodCategory = "validate method";

    private readonly IAspectBuilder<IProperty> _builder;
    private readonly DependencyPropertyAttribute _attribute;
    private readonly DependencyPropertyOptions _options;

    public DependencyPropertyAspectBuilder( IAspectBuilder<IProperty> builder, DependencyPropertyAttribute attribute )
    {
        this._builder = builder;
        this._attribute = attribute;
        this._options = builder.Target.Enhancements().GetOptions<DependencyPropertyOptions>();
    }

    public void Build()
    {
        var builder = this._builder;
        var target = builder.Target;
        var propertyType = builder.Target.Type;
        var declaringType = target.DeclaringType;
        var options = target.Enhancements().GetOptions<DependencyPropertyOptions>();

        var isReadOnly = options.IsReadOnly ?? (this._builder.Target.SetMethod == null || this._builder.Target.Writeability < Writeability.All
                                                                                       || this._builder.Target.SetMethod.Accessibility.IsSubsetOf(
                                                                                           this._builder.Target.GetMethod!.Accessibility ));

        var hasExplicitNaming = this._attribute.PropertyChangedMethod != null
                                || this._attribute.ValidateMethod != null;

        var namingConventions = hasExplicitNaming
            ? [
                new ExplicitDependencyPropertyNamingConvention(
                    this._attribute.RegistrationField,
                    this._attribute.PropertyChangedMethod,
                    this._attribute.ValidateMethod )
            ]
            : options.GetSortedNamingConventions();

        var diagnosticReporter = new DiagnosticReporter( builder );

        if ( !NamingConventionEvaluator.TryEvaluate( namingConventions, target, diagnosticReporter, out var match ) )
        {
            // No match.
            return;
        }

        // NB: WPF convention requires a specific field name, so we don't try to find an unused name.

        IIntroductionAdviceResult<IField>? introduceRegistrationFieldResult = null;
        IIntroductionAdviceResult<IField>? introduceRegistrationKeyFieldResult = null;

        if ( match.RegistrationFieldConflictMatch.Outcome == MemberMatchOutcome.Success )
        {
            introduceRegistrationFieldResult = this._builder.Advice.IntroduceField(
                declaringType,
                match.RegistrationFieldName!,
                typeof(DependencyProperty),
                IntroductionScope.Static,
                OverrideStrategy.Fail,
                b =>
                {
                    // ReSharper disable once RedundantNameQualifier
                    b.Accessibility = MetalamaAccessibility.Public;
                    b.Writeability = Writeability.ConstructorOnly;
                } );

            if ( isReadOnly )
            {
                introduceRegistrationKeyFieldResult = this._builder.Advice.IntroduceField(
                    declaringType,
                    match.RegistrationFieldName! + "Key",
                    typeof(DependencyPropertyKey),
                    IntroductionScope.Static,
                    OverrideStrategy.Fail,
                    b =>
                    {
                        // ReSharper disable once RedundantNameQualifier
                        b.Accessibility = MetalamaAccessibility.Private;
                        b.Writeability = Writeability.ConstructorOnly;
                    } );
            }
        }

        var onChangedMethod = match.PropertyChangedMatch.Member;
        var validateMethod = match.ValidateMatch.Member;

        if ( !MetalamaExecutionContext.Current.ExecutionScenario.CapturesNonObservableTransformations )
        {
            if ( onChangedMethod != null )
            {
                this._builder.Diagnostics.Suppress( Suppressions.SuppressRemoveUnusedPrivateMembersIDE0051, onChangedMethod );
            }

            if ( validateMethod != null )
            {
                this._builder.Diagnostics.Suppress( Suppressions.SuppressRemoveUnusedPrivateMembersIDE0051, validateMethod );
            }

            return;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if ( match == null || introduceRegistrationFieldResult is not { Outcome: AdviceOutcome.Default } )
        {
            // We cannot proceed with other transformations if there was no naming convention match or
            // we could not introduce the DependencyProperty field.

            return;
        }

        // TODO: #34041 - Replace with target.Enhancements().HasAspect<ContractAspect>() once HasAspect supports base types.

        var hasContracts = target.Enhancements().GetAspectInstances().Any( a => typeof(ContractAspect).IsAssignableFrom( a.AspectClass.Type ) );

        IMethod? applyContractsMethod = null;

        if ( hasContracts )
        {
            var name = this.GetAndReserveUnusedMemberName( $"Apply{builder.Target.Name}Contracts" );

            var result = builder.Advice.WithTemplateProvider( Templates.Provider )
                .IntroduceMethod(
                    declaringType,
                    nameof(Templates.ApplyContracts),
                    IntroductionScope.Static,
                    OverrideStrategy.Fail,
                    b =>
                    {
                        b.Name = name;
                        b.Accessibility = MetalamaAccessibility.Private;
                    },
                    new { T = propertyType } );

            if ( result.Outcome != AdviceOutcome.Default )
            {
                return;
            }

            applyContractsMethod = result.Declaration;

            ContractAspect.RedirectContracts( this._builder, target, applyContractsMethod.Parameters[0] );
        }

        this._builder.Advice.WithTemplateProvider( Templates.Provider )
            .AddInitializer(
                declaringType,
                nameof(Templates.InitializeDependencyProperty),
                InitializerKind.BeforeTypeConstructor,
                args: new
                {
                    isReadOnly,
                    dependencyPropertyField = introduceRegistrationFieldResult.Declaration,
                    dependencyPropertyKeyField = introduceRegistrationKeyFieldResult?.Declaration,
                    options = this._options,
                    propertyName = match.DependencyPropertyName,
                    propertyType,
                    declaringType,
                    defaultValueExpr = this._builder.Target.InitializerExpression,
                    onChangedMethod,
                    onChangedSignatureKind = match.PropertyChangedMatch.Kind,
                    validateMethod,
                    validateSignatureKind = match.ValidateMatch.Kind,
                    applyContractsMethod
                } );

        this._builder.Advice.WithTemplateProvider( Templates.Provider )
            .OverrideAccessors(
                this._builder.Target,
                new GetterTemplateSelector( nameof(Templates.OverrideGetter) ),
                args: new { propertyType, dependencyPropertyField = introduceRegistrationFieldResult.Declaration } );

        if ( this._builder.Target.Writeability != Writeability.None )
        {
            this._builder.Advice.WithTemplateProvider( Templates.Provider )
                .OverrideAccessors(
                    this._builder.Target,
                    setTemplate: nameof(Templates.OverrideSetter),
                    args: new { dependencyPropertyField = introduceRegistrationKeyFieldResult?.Declaration ?? introduceRegistrationFieldResult.Declaration } );
        }

        // Here we avoid the temptation to generate a static field to store the result of the initializer expression
        // and use the same result for the default value and as the initial value of all instances of the declaring type. This
        // pattern does not have the same semantics as a regular property initializer, which would be invoked for each instance
        // of the declaring type, while the default value is evaluated just once as this is a statically scoped value.
        // Therefore we evaluate the initializer expression once for the default value and a second time for each object instance.

        if ( this._builder.Target.InitializerExpression != null )
        {
            this._builder.Advice.WithTemplateProvider( Templates.Provider )
                .AddInitializer(
                    declaringType,
                    nameof(Templates.Assign),
                    InitializerKind.BeforeInstanceConstructor,
                    args: new { left = this._builder.Target, right = this._builder.Target.InitializerExpression } );
        }
    }

    private HashSet<string>? _existingMemberNames;

    /// <summary>
    /// Gets an unused member name for the target type by adding an numeric suffix until an unused name is found.
    /// </summary>
    /// <param name="desiredName"></param>
    /// <returns></returns>
    private string GetAndReserveUnusedMemberName( string desiredName )
    {
        this._existingMemberNames ??=
        [
            ..((IEnumerable<INamedDeclaration>) this._builder.Target.DeclaringType.AllMembers()).Concat( this._builder.Target.DeclaringType.Types )
            .Select( m => m.Name )
        ];

        if ( this._existingMemberNames.Add( desiredName ) )
        {
            return desiredName;
        }
        else
        {
            // ReSharper disable once BadSemicolonSpaces
            for ( var i = 1; /* Intentionally empty */; i++ )
            {
                var adjustedName = $"{desiredName}_{i}";

                if ( this._existingMemberNames.Add( adjustedName ) )
                {
                    return adjustedName;
                }
            }
        }
    }
}