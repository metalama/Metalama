// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_Between;

public class OldAttribute : Attribute { }

public class NewAttribute : Attribute { }

public class IntroduceAttributeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var member in builder.Target.Members())
        {
            builder.With( member ).IntroduceAttribute( AttributeConstruction.Create( typeof(NewAttribute) ) );
        }
    }
}

// <target>
[IntroduceAttributeAspect]
internal class IntroduceTarget
{
    // first
    [OldAttribute]

    // second
    private void M() { }
}