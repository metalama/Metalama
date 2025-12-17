// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug828_FieldPseudoAccessors;

#pragma warning disable CS0169

public class TagAttribute : Attribute
{
    public TagAttribute(string tag) { }
}

// This aspect mimics what Multicast does - it introduces an attribute and can target field pseudo accessors
public class AddTagAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        // Apply to all fields
        foreach (var field in builder.Target.Fields)
        {
            // Only introduce attribute on the field itself - not pseudo accessors
            builder.With(field).IntroduceAttribute(
                AttributeConstruction.Create(typeof(TagAttribute), ["field"]));
        }
    }
}

// <target>
[AddTagAspect]
internal class C
{
    private int _field;
}
