// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Runtime.CompilerServices;

// Verifies that the restrictions on the members of a union apply to the declaration form only. The attribute form
// is a class or a struct whose author writes its own storage, so an instance field, an automatic property and a
// field-like event are permitted there, and the advice must not refuse them. See UnionKind, and the tests
// ErrorFieldIntoUnion, ErrorAutomaticPropertyIntoUnion and ErrorFieldLikeEventIntoUnion for the declaration form.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.MembersIntoAttributeUnion
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceField( "_field", typeof(int) );
            builder.IntroduceAutomaticProperty( "Property", typeof(int) );
            builder.IntroduceEvent( nameof(FieldLikeEventTemplate), buildEvent: e => e.Name = "FieldLikeEvent" );
        }

        [Template]
        public event System.EventHandler? FieldLikeEventTemplate;
    }

    // <target>
    [Introduction]
    [Union]
    public class Pet
    {
        public Pet( int cat )
        {
            this.Value = cat;
        }

        public object? Value { get; }
    }
}
