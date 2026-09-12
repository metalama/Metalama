// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.AsField;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceEnum(
            "IntroducedEnum",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "None" );
                e.AddMember( "First" );
            } );

        builder.IntroduceField(
            nameof(FieldTemplate),
            buildField: b =>
            {
                b.Name = "FieldWithIntroducedEnum";
                b.Type = result.Declaration;
            } );

        // An enum is a value type, so its nullable form is Nullable<T> and not an annotated reference type. This is
        // the assertion that a defect in IsReferenceType would break.
        builder.IntroduceField(
            nameof(FieldTemplate),
            buildField: b =>
            {
                b.Name = "NullableFieldWithIntroducedEnum";
                b.Type = result.Declaration.ToNullable();
            } );

        builder.IntroduceProperty(
            nameof(PropertyTemplate),
            buildProperty: b =>
            {
                b.Name = "PropertyWithIntroducedEnum";
                b.Type = result.Declaration;
            } );

        builder.IntroduceMethod(
            nameof(MethodTemplate),
            buildMethod: b =>
            {
                b.Name = "MethodWithIntroducedEnum";
                b.ReturnType = result.Declaration;
                b.AddParameter( "value", result.Declaration );
            } );
    }

    [Template]
    public object? FieldTemplate;

    [Template]
    public object? PropertyTemplate { get; set; }

    [Template]
    public object? MethodTemplate() => default;
}

#pragma warning disable CS8618

// <target>
[Introduction]
public class TargetType { }
