// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// TODO: #33303 The original PS implementation tried to avoid calling GetInvalidTypes at runtime, consider reinstating this behaviour.
// Note that it's not clear exactly why this was done in PS.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Eligibility;
using Metalama.Patterns.Contracts.Numeric;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentOutOfRangeException"/> if the target is assigned a value that
/// is outside a given range.
/// </summary>
/// <remarks>
///     <para>Null values are accepted and do not throw an exception.
/// </para>
/// <para>Error message can use additional argument <value>{4}</value> to refer to the minimum value used and <value>{5}</value> to refer to the maximum value used.</para>
/// </remarks>
/// <seealso href="@contract-types"/>
[PublicAPI]
public class RangeAttribute : ContractBaseAttribute
{
    protected NumericRange Range { get; }

    internal RangeAttribute(
        NumericBound? minValue,
        NumericBound? maxValue )
    {
        this.Range = new NumericRange( minValue, maxValue );
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="long"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param>
    /// <param name="decimalPlaces">When non-zero, interprets the <paramref name="min"/> and <paramref name="max"/> numbers as <see cref="decimal"/> instead
    /// of <see cref="long"/> by adding a decimal point at the specified position. For instance, if <paramref name="min"/> is set to 1234 and <paramref name="decimalPlaces"/>
    /// is set to 3, the <paramref name="min"/> parameter will be reinterpreted as <c>1.234m</c>.</param> 
    public RangeAttribute( long min, long max, bool minAllowed = true, bool maxAllowed = true, int decimalPlaces = 0 ) : this(
        NumericBound.Create( min, minAllowed, decimalPlaces ),
        NumericBound.Create( max, maxAllowed, decimalPlaces ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="ulong"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param> 
    public RangeAttribute( ulong min, ulong max, bool minAllowed = true, bool maxAllowed = true ) : this(
        NumericBound.Create( min, minAllowed ),
        NumericBound.Create( max, maxAllowed ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="double"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param> 
    public RangeAttribute( double min, double max, bool minAllowed = true, bool maxAllowed = true ) : this(
        NumericBound.Create( min, minAllowed ),
        NumericBound.Create( max, maxAllowed ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="float"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param> 
    public RangeAttribute( float min, float max, bool minAllowed = true, bool maxAllowed = true ) : this(
        NumericBound.Create( min, minAllowed ),
        NumericBound.Create( max, maxAllowed ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="int"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param>
    /// <param name="decimalPlaces">When non-zero, interprets the <paramref name="min"/> and <paramref name="max"/> numbers as <see cref="decimal"/> instead
    /// of <see cref="long"/> by adding a decimal point at the specified position. For instance, if <paramref name="min"/> is set to 1234 and <paramref name="decimalPlaces"/>
    /// is set to 3, the <paramref name="min"/> parameter will be reinterpreted as <c>1.234m</c>.</param> 
    public RangeAttribute( int min, int max, bool minAllowed = true, bool maxAllowed = true, int decimalPlaces = 0 ) : this(
        NumericBound.Create( min, minAllowed, decimalPlaces ),
        NumericBound.Create( max, maxAllowed, decimalPlaces ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="uint"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param> 
    public RangeAttribute( uint min, uint max, bool minAllowed = true, bool maxAllowed = true ) : this(
        NumericBound.Create( min, minAllowed ),
        NumericBound.Create( max, maxAllowed ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="byte"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param> 
    public RangeAttribute( byte min, sbyte max, bool minAllowed = true, bool maxAllowed = true ) : this(
        NumericBound.Create( min, minAllowed ),
        NumericBound.Create( max, maxAllowed ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="short"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param> 
    public RangeAttribute( short min, short max, bool minAllowed = true, bool maxAllowed = true ) : this(
        NumericBound.Create( min, minAllowed ),
        NumericBound.Create( max, maxAllowed ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class specifying bounds of type <see cref="ushort"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minAllowed">Determines if the <paramref name="min"/> value is allowed (i.e. if the inequality is <c>&gt;=</c> instead of <c>&gt;</c>.</param>
    /// <param name="maxAllowed">Determines if the <paramref name="max"/> value is allowed (i.e. if the inequality is <c>&lt;=</c> instead of <c>&lt;</c>.</param> 
    public RangeAttribute( ushort min, ushort max, bool minAllowed = true, bool maxAllowed = true ) : this(
        NumericBound.Create( min, minAllowed ),
        NumericBound.Create( max, maxAllowed ) ) { }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            f => IsEligibleType( f.Type ),
            f => $"the type of {f} must be a numeric type, a nullable numeric type, or object" );
    }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IParameter> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            p => IsEligibleType( p.Type ),
            p => $"the type of {p} must be a numeric type, a nullable numeric type, or object" );
    }

    [CompileTime]
    private static bool IsEligibleType( IType type )
        => type.ToNonNullable().SpecialType switch
        {
            SpecialType.UInt16 or
                SpecialType.UInt32 or
                SpecialType.UInt64 or
                SpecialType.Int16 or
                SpecialType.Int32 or
                SpecialType.Int64 or
                SpecialType.Byte or
                SpecialType.SByte or
                SpecialType.Single or
                SpecialType.Double or
                SpecialType.Decimal or
                SpecialType.Object => true,
            _ => false
        };

    private (NumericRange Range, InequalityAmbiguity? Ambiguity) ResolveRange( ContractContext context )
    {
        // If the attribute has an ambiguous meaning for historical reasons, report a warning unless
        // the user has specified how to resolve the ambiguity.
        var ambiguity = this.Ambiguity;

        if ( ambiguity != null )
        {
            var defaultStrictness = context.TargetDeclaration.GetContractOptions().DefaultInequalityStrictness;

            if ( defaultStrictness != null )
            {
                // The ambiguity is resolved.
                ambiguity = null;
            }

            if ( this.Ambiguity != null && context.Options.DefaultInequalityStrictness != null )
            {
                return (this.Range.WithStrictness( context.Options.DefaultInequalityStrictness.Value ), ambiguity);
            }
        }

        return (this.Range, ambiguity);
    }

    private void BuildAspect( IAspectBuilder builder, IType targetType )
    {
        var resolvedRange = this.ResolveRange( new ContractContext( builder.Target, ContractDirection.None ) );

        if ( resolvedRange.Ambiguity != null )
        {
            var attribute = builder.AspectInstance.Predecessors[0].Instance as IAttribute;

            builder.Diagnostics.Report(
                ContractDiagnostics.AttributeMeaningIsAmbiguous.WithArguments(
                    (builder.Target,
                     this.GetType().Name,
                     resolvedRange.Ambiguity.NewName1, resolvedRange.Ambiguity.NewName2, resolvedRange.Ambiguity.DefaultStrictness) ),
                attribute ?? builder.Target );
        }

        var basicType = (INamedType) targetType.ToNonNullable();

        switch ( this.Range.IsTypeSupported( basicType ) )
        {
            case NumericRangeTypeSupport.NotSupported:
                builder.Diagnostics.Report(
                    ContractDiagnostics.RangeCannotBeApplied
                        .WithArguments(
                            (builder.Target,
                             basicType.Name,
                             builder.AspectInstance.AspectClass.ShortName,
                             resolvedRange.Range) ) );

                builder.SkipAspect();

                break;

            case NumericRangeTypeSupport.Redundant:
                builder.Diagnostics.Report(
                    ContractDiagnostics.RangeIsRedundant
                        .WithArguments(
                            (builder.Target,
                             basicType.Name,
                             builder.AspectInstance.AspectClass.ShortName,
                             resolvedRange.Range) ) );

                builder.SkipAspect();

                break;
        }
    }

    /// <inheritdoc/>
    public override void BuildAspect( IAspectBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        base.BuildAspect( builder );
        this.BuildAspect( builder, builder.Target.Type );
    }

    /// <inheritdoc/>
    public override void BuildAspect( IAspectBuilder<IParameter> builder )
    {
        base.BuildAspect( builder );
        this.BuildAspect( builder, builder.Target.Type );
    }

    /// <inheritdoc/>
    public override void Validate( dynamic? value )
    {
        var expressionBuilder = new ExpressionBuilder();
        var expression = (IExpression) value!;

        var context = new ContractContext( meta.Target );
        var range = this.ResolveRange( context ).Range;
        var type = context.Type;

        if ( range.GeneratePattern( type, expressionBuilder, expression ) )
        {
            if ( expressionBuilder.ToValue() )
            {
                this.OnContractViolated( value, range, context );
            }
        }
        else
        {
            meta.InsertComment( $"The {range} validation on {expression} is fully redundant and has been skipped." );
        }
    }

    [Template]
    protected virtual void OnContractViolated( dynamic? value, [CompileTime] NumericRange range, [CompileTime] ContractContext context )
    {
        context.Options.Templates!.OnRangeContractViolated( value, range, context );
    }

    private protected virtual InequalityAmbiguity? Ambiguity => null;
}