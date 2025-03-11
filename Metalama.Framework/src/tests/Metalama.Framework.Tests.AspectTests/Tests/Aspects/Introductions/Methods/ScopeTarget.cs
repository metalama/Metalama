// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.ScopeTarget;

public class TheAspect : Attribute, IAspect<IDeclaration>
{
    public void BuildAspect( IAspectBuilder<IDeclaration> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }

    [Introduce( Scope = IntroductionScope.Target )]
    public void IntroducedMethod() { }
}

[TheAspect]
public class InstanceClass { }

[TheAspect]
public static class StaticClass { }

public class Class1
{
    [TheAspect]
    public void InstanceMember() { }
}

public class Class3
{
    // The class is intentionally not static.

    [TheAspect]
    public static void StaticMember() { }
}