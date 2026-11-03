// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Adviser.With_DeclaringType;

// WithTemplateProvider returns an adviser for C.M. WithDeclaringType calls With with C, which contains C.M, so it must succeed.
public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.WithTemplateProvider( new Templates() ).WithDeclaringType().IntroduceField( "_introducedField", typeof(int) );
    }
}

public class Templates : ITemplateProvider { }

// <target>
internal class C
{
    [MyAspect]
    public void M() { }
}
