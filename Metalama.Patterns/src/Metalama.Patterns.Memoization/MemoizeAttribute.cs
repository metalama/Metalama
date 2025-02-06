// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Eligibility;
using System.Runtime.CompilerServices;

namespace Metalama.Patterns.Memoization;

/// <summary>
/// Custom attribute that, when added to a get-only property or non-void parameterless method, causes the result of this property or method
/// to be indefinitely stored. All calls to the target property or method are guaranteed to return the same value or object reference.
/// The underlying implementation of the property or method is not guaranteed to be executed only once. The cached result is stored in the type itself.
/// </summary>
[PublicAPI]
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Method )]
public sealed class MemoizeAttribute : Attribute, IAspect<IMethod>, IAspect<IProperty>
{
    void IEligible<IMethod>.BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        BuildCommonEligibility( builder );
        builder.AddRule( EligibilityRuleFactory.GetAdviceEligibilityRule( AdviceKind.OverrideMethod ) );
        builder.MustSatisfy( m => !m.IsReadOnly, m => $"{m} must not be readonly" );
        builder.MustSatisfy( m => m.MethodKind == MethodKind.Default, m => $"{m} must be a normal method" );
        builder.ReturnType().MustSatisfy( t => t.SpecialType != SpecialType.Void, t => $"{t} must not be void" );
        builder.MustSatisfy( m => m.Parameters.Count == 0, m => $"{m} must not have any parameters" );
    }

    void IEligible<IProperty>.BuildEligibility( IEligibilityBuilder<IProperty> builder )
    {
        BuildCommonEligibility( builder );
        builder.AddRule( EligibilityRuleFactory.GetAdviceEligibilityRule( AdviceKind.OverrideFieldOrPropertyOrIndexer ) );
        builder.MustSatisfy( p => p.Writeability == Writeability.None, p => $"{p} must not have a setter" );
    }

    private static void BuildCommonEligibility( IEligibilityBuilder<IMember> builder )
    {
        builder.DeclaringType().MustSatisfy( t => !t.IsReadOnly, t => $"{t} must not be readonly" );
    }

    void IAspect<IMethod>.BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var fieldName = "_" + builder.Target.Name;

        if ( builder.Target.ReturnType is { IsReferenceType: true, IsNullable: false } )
        {
            var field = builder.Advice.IntroduceField( builder.Target.DeclaringType, fieldName, builder.Target.ReturnType, IntroductionScope.Target )
                .Declaration;

            builder.Advice.Override( builder.Target, nameof(this.NonNullableReferenceTypeTemplate), args: new { field } );
        }
        else
        {
            var fieldType = ((INamedType) TypeFactory.GetType( typeof(StrongBox<>) )).WithTypeArguments( builder.Target.ReturnType );

            var field = builder.Advice.IntroduceField( builder.Target.DeclaringType, fieldName, fieldType, IntroductionScope.Target ).Declaration;
            builder.Advice.Override( builder.Target, nameof(this.BoxingTemplate), args: new { field, T = builder.Target.ReturnType } );
        }
    }

    void IAspect<IProperty>.BuildAspect( IAspectBuilder<IProperty> builder )
    {
        var fieldName = "_" + builder.Target.Name;

        if ( builder.Target.Type is { IsReferenceType: true, IsNullable: false } )
        {
            var field = builder.Advice.IntroduceField( builder.Target.DeclaringType, fieldName, builder.Target.Type, IntroductionScope.Target ).Declaration;
            builder.Advice.OverrideAccessors( builder.Target, getTemplate: nameof(this.NonNullableReferenceTypeTemplate), args: new { field } );
        }
        else
        {
            var fieldType = ((INamedType) TypeFactory.GetType( typeof(StrongBox<>) )).WithTypeArguments( builder.Target.Type );

            var field = builder.Advice.IntroduceField( builder.Target.DeclaringType, fieldName, fieldType, IntroductionScope.Target ).Declaration;
            builder.Advice.OverrideAccessors( builder.Target, getTemplate: nameof(this.BoxingTemplate), args: new { field, T = builder.Target.Type } );
        }
    }

    [Template]
    private dynamic? NonNullableReferenceTypeTemplate( IField field )
    {
        if ( field.Value == null )
        {
            var value = meta.Proceed();

            var statementBuilder = new StatementBuilder();
            statementBuilder.AppendTypeName( typeof(Interlocked) );
            statementBuilder.AppendVerbatim( "." );
            statementBuilder.AppendVerbatim( nameof(Interlocked.CompareExchange) );
            statementBuilder.AppendVerbatim( "( ref " );
            statementBuilder.AppendExpression( field );
            statementBuilder.AppendVerbatim( ", " );
            statementBuilder.AppendExpression( value );
            statementBuilder.AppendVerbatim( ", null );" );

            meta.InsertStatement( statementBuilder.ToStatement() );
        }

        return field.Value;
    }

    [Template]
    private T BoxingTemplate<[CompileTime] T>( IField field )
    {
        if ( field.Value == null )
        {
            var value = new StrongBox<T>( meta.Proceed()! );

            var statementBuilder = new StatementBuilder();
            statementBuilder.AppendTypeName( typeof(Interlocked) );
            statementBuilder.AppendVerbatim( "." );
            statementBuilder.AppendVerbatim( nameof(Interlocked.CompareExchange) );
            statementBuilder.AppendVerbatim( "( ref " );
            statementBuilder.AppendExpression( field );
            statementBuilder.AppendVerbatim( ", " );
            statementBuilder.AppendExpression( value );
            statementBuilder.AppendVerbatim( ", null );" );

            meta.InsertStatement( statementBuilder.ToStatement() );
        }

        return field.Value!.Value;
    }
}