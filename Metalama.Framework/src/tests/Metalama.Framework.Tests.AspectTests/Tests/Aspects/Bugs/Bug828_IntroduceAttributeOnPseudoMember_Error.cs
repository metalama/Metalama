// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug828_IntroduceAttributeOnPseudoMember_Error;

#pragma warning disable CS0169

public class TagAttribute : Attribute
{
    public TagAttribute(string tag) { }
}

// This aspect attempts to introduce an attribute on a pseudo accessor, which should fail with a meaningful error
public class AddTagAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        foreach (var field in builder.Target.Fields)
        {
            // Try to introduce attribute on field pseudo accessors - this should produce an error
            if (field.GetMethod != null)
            {
                builder.With(field.GetMethod).IntroduceAttribute(
                    AttributeConstruction.Create(typeof(TagAttribute), ["getter"]));
            }
        }
    }
}

// <target>
[AddTagAspect]
internal class C
{
    private int _field;
}
