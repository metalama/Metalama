// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33018;

public abstract class IntroductionAttribute : TypeAspect
{
    [Introduce( IsVirtual = false )]
    public virtual void M()
    {
        Console.WriteLine( "Base template (wrong)." );
    }
}

public class InheritedIntroductionAttribute : IntroductionAttribute
{
    public override void M()
    {
        Console.WriteLine( "Override template (expected)." );
    }
}

// <target>
[InheritedIntroduction]
internal class TargetClass { }