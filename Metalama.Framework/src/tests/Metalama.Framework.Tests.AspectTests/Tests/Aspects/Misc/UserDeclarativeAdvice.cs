// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.UserDeclarativeAdvice;

[AttributeUsage( AttributeTargets.Field )]
public class MyAdviceAttribute : DeclarativeAdviceAttribute, ITemplateAttribute
{
    public override void BuildAdvice( IMemberOrNamedType templateMember, string templateMemberId, IAspectBuilder<IDeclaration> builder )
    {
        builder.With( (INamedType)builder.Target ).IntroduceField( templateMemberId );
    }

    TemplateAttributeProperties? ITemplateAttribute.Properties => null;
}

public class MyAspect : TypeAspect
{
    [MyAdvice]
    private int f;
}

// <target>
[MyAspect]
public class Target { }