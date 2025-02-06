// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentException"/> if the target is assigned a value that
/// is not a valid member of an enumeration. 
/// </summary>
/// <remarks>
///     <para>Strings are matched against enumeration member names. Integers
/// are matched against enumeration member values. If the target is a nullable type, null values are accepted and do not
/// throw exception.
/// </para>
/// </remarks>
/// <seealso href="@contract-types"/>
[PublicAPI]
public sealed class EnumDataTypeAttribute : ContractBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumDataTypeAttribute"/> class.
    /// </summary>
    /// <param name="enumType">Enumeration type.</param>
    public EnumDataTypeAttribute( Type enumType )
    {
        this.EnumType = enumType;
    }

    /// <summary>
    /// Gets the enumeration type.
    /// </summary>
    public Type EnumType { get; }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            f => IsEligibleType( f.Type ),
            f => $"the type of {f} must be string, an integer type or a nullable integer type" );
    }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IParameter> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            p => IsEligibleType( p.Type ),
            p => $"the type of {p} must be string, an integer type or a nullable integer type" );
    }

    [CompileTime]
    private static bool IsEligibleType( IType type )
        => type.ToNonNullable().SpecialType switch
        {
            SpecialType.String or
                SpecialType.UInt16 or
                SpecialType.UInt32 or
                SpecialType.UInt64 or
                SpecialType.Int16 or
                SpecialType.Int32 or
                SpecialType.Int64 or
                SpecialType.Byte or
                SpecialType.SByte or
                SpecialType.Object => true,
            _ => false
        };

    /// <inheritdoc/>
    public override void Validate( dynamic? value )
    {
        var context = new ContractContext( meta.Target );
        var targetType = context.Type;
        var templates = context.Options.Templates!;

        if ( targetType.SpecialType is SpecialType.String or SpecialType.Object )
        {
            if ( value != null! && !EnumDataTypeAttributeHelper.IsValidEnumValue( value, this.EnumType ) )
            {
                templates.OnInvalidEnumValue( value, context );
            }
        }
        else if ( targetType.IsNullable == true )
        {
            // target is a nullable integer type.
            if ( value!.HasValue )
            {
                var enumValue = Enum.ToObject( this.EnumType, value );

                if ( !EnumDataTypeAttributeHelper.IsValidEnumValue( enumValue, this.EnumType ) )
                {
                    templates.OnInvalidEnumValue( value, context );
                }
            }
        }
        else
        {
            // target is a non-nullable integer type.
            var enumValue = Enum.ToObject( this.EnumType, value );

            if ( !EnumDataTypeAttributeHelper.IsValidEnumValue( enumValue, this.EnumType ) )
            {
                templates.OnInvalidEnumValue( value, context );
            }
        }
    }
}