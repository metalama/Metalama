// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Delegates.AsEventType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var handler = builder.IntroduceDelegate(
            "ValueChangedHandler",
            buildDelegate: d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                d.AddParameter( "oldValue", typeof(object) );
                d.AddParameter( "newValue", typeof(object) );
            } );

        // The type of an event must be a delegate, which is the reader that section 5.1 of
        // Metalama.Framework/docs/introducing-types.md names, so an introduced delegate has to work here.
        builder.IntroduceEvent(
            nameof(EventTemplate),
            buildEvent: e =>
            {
                e.Name = "ValueChanged";
                e.Type = handler.Declaration;
            } );

        builder.IntroduceField(
            nameof(FieldTemplate),
            buildField: b =>
            {
                b.Name = "_onValueChanged";
                b.Type = handler.Declaration;
            } );

        // A delegate is a reference type, so its nullable form is an annotated reference type and not
        // Nullable<T>. This is the assertion that a defect in IsReferenceType would break.
        builder.IntroduceField(
            nameof(FieldTemplate),
            buildField: b =>
            {
                b.Name = "_nullableOnValueChanged";
                b.Type = handler.Declaration.ToNullable();
            } );
    }

    [Template]
    public event System.EventHandler? EventTemplate
    {
        add { }
        remove { }
    }

    [Template]
    public object? FieldTemplate;
}

#pragma warning disable CS8618

// <target>
[Introduction]
public class TargetType { }
