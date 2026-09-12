// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Builds a member of an enum that an advice introduces.
/// </summary>
/// <remarks>
/// <para>
/// A member of an enum is a constant field whose type is the enum, so the class derives from
/// <see cref="FieldBuilder"/> and fixes everything the language fixes. The interface it exposes to an aspect is
/// <see cref="IEnumMemberBuilder"/>, which carries the custom attributes and nothing else, because the name and the
/// value of a member are arguments of <c>AddMember</c>.
/// </para>
/// </remarks>
internal sealed class EnumMemberBuilder : FieldBuilder, IEnumMemberBuilder
{
    public EnumMemberBuilder( AspectLayerInstance aspectLayerInstance, EnumBuilder declaringEnum, string name, TypedConstant? value )
        : base( aspectLayerInstance, declaringEnum, name )
    {
        // The type of a member of an enum is the enum itself, its accessibility is that of the enum, and it is a
        // constant, which Writeability.None reports. The language fixes all three.
        this.Type = declaringEnum;
        this.Accessibility = Accessibility.Public;
        this.IsStatic = true;
        this.Writeability = Writeability.None;
        this.ConstantValue = value;
    }
}
