// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to add custom attributes to a member of an enum that has been created by one of the <c>AddMember</c>
/// methods of <see cref="IEnumBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// This interface declares no member of its own. It exists for the custom attributes that
/// <see cref="IDeclarationBuilder"/> declares, which are the only thing about a member of an enum that remains to be
/// chosen after it is added: its name and its value are arguments of <c>AddMember</c>.
/// </para>
/// <para>
/// A member of an enum is a constant field, and the code model reports it as an <see cref="IField"/> once the enum is
/// introduced. This interface is nevertheless not an <see cref="IFieldBuilder"/>: a member of an enum has no type of
/// its own, no accessibility of its own, no initializer and no modifier, so almost every operation of a field builder
/// would be invalid on it.
/// </para>
/// </remarks>
/// <seealso cref="IEnumBuilder"/>
/// <seealso cref="IField"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IEnumMemberBuilder : IDeclarationBuilder, INamedDeclaration;
