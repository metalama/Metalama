// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of an enum that has been created by an advice.
/// </summary>
/// <remarks>
/// <para>
/// The following inherited properties are not valid for an enum, and their setter throws a
/// <see cref="NotSupportedException"/>: <see cref="IMemberOrNamedTypeBuilder.IsStatic"/>,
/// <see cref="IMemberOrNamedTypeBuilder.IsSealed"/>, <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/> and
/// <see cref="IMemberOrNamedTypeBuilder.IsPartial"/>. An enum is implicitly sealed, it may not be abstract or
/// static, and the language has no partial enum.
/// </para>
/// <para>
/// An enum builder is not an <see cref="INamedType"/>, so it may not be used where an <see cref="IType"/> is
/// expected. The introduced enum is read from
/// <see cref="Metalama.Framework.Advising.IIntroductionAdviceResult{T}.Declaration"/>.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Advising.IAdviceFactory.IntroduceEnum"/>
/// <seealso cref="Metalama.Framework.Aspects.AdviserExtensions.IntroduceEnum"/>
/// <seealso cref="IEnumMemberBuilder"/>
/// <seealso cref="Metalama.Framework.Code.Types.IEnumFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IEnumBuilder : IMemberOrNamedTypeBuilder
{
    /// <summary>
    /// Gets or sets the underlying integral type of the enum. The default value is <see cref="SpecialType.Int32"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language allows one of <see cref="SpecialType.Byte"/>, <see cref="SpecialType.SByte"/>,
    /// <see cref="SpecialType.Int16"/>, <see cref="SpecialType.UInt16"/>, <see cref="SpecialType.Int32"/>,
    /// <see cref="SpecialType.UInt32"/>, <see cref="SpecialType.Int64"/> and <see cref="SpecialType.UInt64"/>. The
    /// setter throws an <see cref="ArgumentOutOfRangeException"/> for any other value.
    /// </para>
    /// <para>
    /// Setting this property after a member has been added throws an <see cref="InvalidOperationException"/>,
    /// because the value of a member is validated against the underlying type when the member is added.
    /// </para>
    /// </remarks>
    /// <seealso cref="Metalama.Framework.Code.Types.IEnumFacet.UnderlyingType"/>
    SpecialType UnderlyingType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the enum is annotated with <see cref="System.FlagsAttribute"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting this property to <c>true</c> adds the attribute, and setting it to <c>false</c> removes it. Adding
    /// the attribute through <see cref="IDeclarationBuilder.AddAttribute"/> has the same effect, and this property
    /// then reports <c>true</c>.
    /// </para>
    /// <para>
    /// The attribute changes how the value of the enum is formatted and parsed at run time, and it does not change
    /// the value of any member. An author who sets this property assigns the value of every member explicitly,
    /// because neither the language nor Metalama derives a power of two from the position of a member.
    /// </para>
    /// </remarks>
    /// <seealso cref="Metalama.Framework.Code.Types.IEnumFacet.IsFlags"/>
    bool IsFlags { get; set; }

    /// <summary>
    /// Gets the members that have been added so far, in the order in which they were added.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IEnumFacet.Members"/>
    IReadOnlyList<IEnumMemberBuilder> Members { get; }

    /// <summary>
    /// Adds a member whose value the language assigns, which is zero for the first member and the value of the
    /// preceding member plus one for any other.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name must not be the name of a member already added, and a duplicate throws an
    /// <see cref="ArgumentException"/>. The members are declared in the order in which they were added.
    /// </para>
    /// </remarks>
    /// <param name="name">The name of the member.</param>
    /// <returns>An <see cref="IEnumMemberBuilder"/> that allows you to add custom attributes to the new member.</returns>
    IEnumMemberBuilder AddMember( string name );

    /// <summary>
    /// Adds a member of the given value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name must not be the name of a member already added, and a duplicate throws an
    /// <see cref="ArgumentException"/>. The value is converted to <see cref="UnderlyingType"/>, and a value that
    /// does not fit in that type throws an <see cref="ArgumentOutOfRangeException"/>. The members are declared in
    /// the order in which they were added.
    /// </para>
    /// </remarks>
    /// <param name="name">The name of the member.</param>
    /// <param name="value">The value of the member, converted to <see cref="UnderlyingType"/>.</param>
    /// <returns>An <see cref="IEnumMemberBuilder"/> that allows you to add custom attributes to the new member.</returns>
    IEnumMemberBuilder AddMember( string name, sbyte value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, byte value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, short value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, ushort value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, int value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, uint value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, long value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, ulong value );

    /// <summary>
    /// Adds a member whose value is given as a <see cref="TypedConstant"/>, which is the form in which the value of
    /// a member of another enum is read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name must not be the name of a member already added, and a duplicate throws an
    /// <see cref="ArgumentException"/>. The type of the constant must be an integral type or an enum, and any other
    /// type throws an <see cref="ArgumentException"/> as well. This overload therefore accepts the value of a member
    /// of another enum directly, which is how such a value is read from the code model.
    /// </para>
    /// <para>
    /// The value is converted to <see cref="UnderlyingType"/> and the type of the constant is replaced by it,
    /// including when the constant came from another enum. The language converts neither one enum to another nor an
    /// enum to an integral type implicitly, so a member that kept the type of the constant it came from would not
    /// compile. A value that does not fit in the underlying type throws an
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </para>
    /// <para>
    /// An uninitialized constant, and a constant whose value is <c>null</c>, add a member whose value the language
    /// assigns, exactly as <see cref="AddMember(string)"/> does.
    /// </para>
    /// </remarks>
    /// <param name="name">The name of the member.</param>
    /// <param name="value">The value of the member, converted to <see cref="UnderlyingType"/>.</param>
    /// <returns>An <see cref="IEnumMemberBuilder"/> that allows you to add custom attributes to the new member.</returns>
    IEnumMemberBuilder AddMember( string name, TypedConstant value );
}
