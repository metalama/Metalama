// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Builds an enum that an advice introduces.
/// </summary>
/// <remarks>
/// <para>
/// The class derives from <see cref="NamedTypeBuilder"/>, because the compilation model requires an
/// <c>INamedTypeImpl</c>, while the interface it exposes to an aspect is <see cref="IEnumBuilder"/>, which does not
/// derive from <see cref="INamedType"/>. The public interface is therefore a narrowed view of an object that is a
/// named type internally.
/// </para>
/// <para>
/// A member of an enum is a constant field. It is created as a <see cref="FieldBuilder"/> here and registered by the
/// advice; the enum declaration emits the members itself, which makes this kind the exception to section 4.2 of
/// <c>Metalama.Framework/docs/future/introducing-types.md</c>, because the members of an enum are written by the
/// aspect author rather than synthesized by the compiler.
/// </para>
/// </remarks>
internal sealed class EnumBuilder : NamedTypeBuilder, IEnumBuilder
{
    private readonly List<EnumMemberBuilder> _members = [];
    private INamedType _underlyingType;

    public EnumBuilder( AspectLayerInstance aspectLayerInstance, INamespaceOrNamedType declaringNamespaceOrType, string name )
        : base( aspectLayerInstance, declaringNamespaceOrType, name, TypeKind.Enum )
    {
        this._underlyingType = this.Compilation.Factory.GetSpecialType( SpecialType.Int32 );
    }

    public override INamedType UnderlyingType => this._underlyingType;

    INamedType IEnumBuilder.UnderlyingType
    {
        get => this._underlyingType;
        set
        {
            this.CheckNotFrozen();

            if ( !IsValidUnderlyingType( value ) )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"The type '{value}' cannot be the underlying type of an enum. The language allows byte, sbyte, short, ushort, int, uint, long and ulong." );
            }

            if ( this._members.Count > 0 )
            {
                throw new InvalidOperationException(
                    $"The underlying type of the enum '{this.Name}' cannot be changed because a member has already been added, and the value of a member is validated against the underlying type when the member is added." );
            }

            this._underlyingType = value;
        }
    }

    /// <summary>
    /// Determines whether a type is one of the eight integral types that the language allows as the underlying type of
    /// an enum.
    /// </summary>
    private static bool IsValidUnderlyingType( INamedType type )
        => type.SpecialType is SpecialType.Byte or SpecialType.SByte or SpecialType.Int16 or SpecialType.UInt16 or SpecialType.Int32 or SpecialType.UInt32
            or SpecialType.Int64 or SpecialType.UInt64;

    /// <summary>
    /// Gets the type of <see cref="FlagsAttribute"/> in the compilation under construction.
    /// </summary>
    private INamedType FlagsAttributeType => this.Compilation.Factory.GetTypeByReflectionName( "System.FlagsAttribute" );

    public bool IsFlags
    {
        // The property and the attribute are one state and not two: the setter adds or removes the attribute, and the
        // getter reports whether it is present, so adding it through AddAttribute makes this property report true.
        get => this.Attributes.OfAttributeType( this.FlagsAttributeType ).Any();
        set
        {
            this.CheckNotFrozen();

            if ( value )
            {
                if ( !this.IsFlags )
                {
                    this.AddAttribute( AttributeConstruction.Create( this.FlagsAttributeType ) );
                }
            }
            else
            {
                this.RemoveAttributes( this.FlagsAttributeType );
            }
        }
    }

    /// <summary>
    /// Always <c>false</c>. The setter throws a <see cref="NotSupportedException"/>, because the language has no
    /// static enum.
    /// </summary>
    public override bool IsStatic
    {
        get => false;
        set => throw NotSupported( nameof(this.IsStatic), "an enum cannot be static" );
    }

    /// <summary>
    /// Always <c>false</c>. The setter throws a <see cref="NotSupportedException"/>, because the language has no
    /// abstract enum.
    /// </summary>
    public override bool IsAbstract
    {
        get => false;
        set => throw NotSupported( nameof(this.IsAbstract), "an enum cannot be abstract" );
    }

    /// <summary>
    /// Always <c>true</c>. The setter throws a <see cref="NotSupportedException"/>, because an enum is implicitly
    /// sealed and the language refuses the modifier.
    /// </summary>
    public override bool IsSealed
    {
        get => true;
        set => throw NotSupported( nameof(this.IsSealed), "an enum is implicitly sealed" );
    }

    /// <summary>
    /// Always <c>false</c>. The setter throws a <see cref="NotSupportedException"/>, because the language has no
    /// partial enum.
    /// </summary>
    public override bool IsPartial
    {
        get => false;
        set => throw NotSupported( nameof(this.IsPartial), "the language has no partial enum" );
    }

    /// <summary>
    /// Builds the exception that the setter of a property that is not valid for an enum throws.
    /// </summary>
    private static NotSupportedException NotSupported( string propertyName, string reason )
        => new( $"The '{propertyName}' property cannot be set on an enum builder, because {reason}." );

    public IReadOnlyList<IEnumMemberBuilder> Members => this._members;

    /// <summary>
    /// Gets the members as the builders that the advice registers in the code model and that the enum declaration
    /// emits.
    /// </summary>
    public IReadOnlyList<EnumMemberBuilder> MemberBuilders => this._members;

    public IEnumMemberBuilder AddMember( string name ) => this.AddMemberCore( name, null );

    public IEnumMemberBuilder AddMember( string name, sbyte value ) => this.AddIntegralMember( name, value );

    public IEnumMemberBuilder AddMember( string name, byte value ) => this.AddIntegralMember( name, value );

    public IEnumMemberBuilder AddMember( string name, short value ) => this.AddIntegralMember( name, value );

    public IEnumMemberBuilder AddMember( string name, ushort value ) => this.AddIntegralMember( name, value );

    public IEnumMemberBuilder AddMember( string name, int value ) => this.AddIntegralMember( name, value );

    public IEnumMemberBuilder AddMember( string name, uint value ) => this.AddIntegralMember( name, value );

    public IEnumMemberBuilder AddMember( string name, long value ) => this.AddIntegralMember( name, value );

    public IEnumMemberBuilder AddMember( string name, ulong value ) => this.AddUnsignedMember( name, value );

    public IEnumMemberBuilder AddMember( string name, TypedConstant value )
    {
        if ( !value.IsInitialized || value.Value == null )
        {
            return this.AddMemberCore( name, null );
        }

        if ( value.Type is not INamedType namedType || !(namedType.IsEnum || IsValidUnderlyingType( namedType )) )
        {
            throw new ArgumentException(
                $"The value of the member '{name}' of the enum '{this.Name}' must be of an integral type or of an enum, and '{value.Type}' is neither.",
                nameof(value) );
        }

        // The value of a constant of an enum is its underlying value, so a constant read from another enum and a
        // constant of an integral type are converted the same way. The type of the constant is replaced by the
        // underlying type of this enum, because the language converts neither one enum to another nor an enum to an
        // integral type implicitly, and the declaration would not compile otherwise.
        return value.Value switch
        {
            ulong unsignedValue => this.AddUnsignedMember( name, unsignedValue ),
            _ => this.AddIntegralMember( name, Convert.ToInt64( value.Value, CultureInfo.InvariantCulture ) )
        };
    }

    /// <summary>
    /// Adds a member whose value is given as a signed integral value, which every overload but the one that takes
    /// <see cref="ulong"/> reaches.
    /// </summary>
    private IEnumMemberBuilder AddIntegralMember( string name, long value )
    {
        // The magnitude is computed by negating in unsigned arithmetic rather than through Math.Abs, which throws for
        // long.MinValue, whose magnitude is one more than long.MaxValue and therefore not a long.
        var magnitude = value < 0 ? ~(ulong) value + 1 : (ulong) value;

        this.CheckValueFitsInUnderlyingType( name, value < 0, magnitude );

        return this.AddMemberCore( name, TypedConstant.Create( ConvertToUnderlyingType( value, this._underlyingType ), this._underlyingType ) );
    }

    /// <summary>
    /// Adds a member whose value is given as a <see cref="ulong"/>, which is the one value that
    /// <see cref="AddIntegralMember"/> cannot express, because a value above <see cref="long.MaxValue"/> does not fit
    /// in a <see cref="long"/>.
    /// </summary>
    private IEnumMemberBuilder AddUnsignedMember( string name, ulong value )
    {
        this.CheckValueFitsInUnderlyingType( name, false, value );

        return this.AddMemberCore( name, TypedConstant.Create( ConvertToUnsignedUnderlyingType( value, this._underlyingType ), this._underlyingType ) );
    }

    private void CheckValueFitsInUnderlyingType( string name, bool isNegative, ulong magnitude )
    {
        // The first element is the magnitude of the most negative value the type can hold, which is zero for an
        // unsigned type, and the second is the largest value it can hold.
        (ulong MinMagnitude, ulong Max) range = this._underlyingType.SpecialType switch
        {
            SpecialType.Byte => (0UL, byte.MaxValue),
            SpecialType.SByte => ((ulong) sbyte.MaxValue + 1, (ulong) sbyte.MaxValue),
            SpecialType.Int16 => ((ulong) short.MaxValue + 1, (ulong) short.MaxValue),
            SpecialType.UInt16 => (0UL, ushort.MaxValue),
            SpecialType.Int32 => ((ulong) int.MaxValue + 1, int.MaxValue),
            SpecialType.UInt32 => (0UL, uint.MaxValue),
            SpecialType.Int64 => ((ulong) long.MaxValue + 1, long.MaxValue),
            SpecialType.UInt64 => (0UL, ulong.MaxValue),
            _ => throw new AssertionFailedException( $"Unsupported underlying type '{this._underlyingType}'." )
        };

        var fits = isNegative ? range.MinMagnitude > 0 && magnitude <= range.MinMagnitude : magnitude <= range.Max;

        if ( !fits )
        {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                $"The value of the member '{name}' does not fit in '{this._underlyingType}', which is the underlying type of the enum '{this.Name}'." );
        }
    }

    private static object ConvertToUnderlyingType( long value, INamedType underlyingType )
        => underlyingType.SpecialType switch
        {
            SpecialType.Byte => (byte) value,
            SpecialType.SByte => (sbyte) value,
            SpecialType.Int16 => (short) value,
            SpecialType.UInt16 => (ushort) value,
            SpecialType.Int32 => (int) value,
            SpecialType.UInt32 => (uint) value,
            SpecialType.Int64 => value,
            SpecialType.UInt64 => (ulong) value,
            _ => throw new AssertionFailedException( $"Unsupported underlying type '{underlyingType}'." )
        };

    private static object ConvertToUnsignedUnderlyingType( ulong value, INamedType underlyingType )
        => underlyingType.SpecialType switch
        {
            SpecialType.Byte => (byte) value,
            SpecialType.SByte => (sbyte) value,
            SpecialType.Int16 => (short) value,
            SpecialType.UInt16 => (ushort) value,
            SpecialType.Int32 => (int) value,
            SpecialType.UInt32 => (uint) value,
            SpecialType.Int64 => (long) value,
            SpecialType.UInt64 => value,
            _ => throw new AssertionFailedException( $"Unsupported underlying type '{underlyingType}'." )
        };

    private IEnumMemberBuilder AddMemberCore( string name, TypedConstant? value )
    {
        this.CheckNotFrozen();

        if ( this._members.Any( m => m.Name == name ) )
        {
            throw new ArgumentException( $"The enum '{this.Name}' already declares a member named '{name}'.", nameof(name) );
        }

        var member = new EnumMemberBuilder( this.AspectLayerInstance, this, name, value );
        this._members.Add( member );

        return member;
    }

    protected override void FreezeChildren()
    {
        base.FreezeChildren();

        foreach ( var member in this._members )
        {
            member.Freeze();
        }
    }
}
