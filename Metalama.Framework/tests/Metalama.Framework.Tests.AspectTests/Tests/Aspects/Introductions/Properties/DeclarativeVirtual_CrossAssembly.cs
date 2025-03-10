// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.DeclarativeVirtual_CrossAssembly;

[RunTimeOrCompileTime]
public class InheritedIntroductionAttribute : IntroductionAttribute
{
    public override int VirtualOverriddenIntroduction
    {
        get
        {
            Console.WriteLine( "Base template (expected)." );

            return 42;
        }
        set
        {
            Console.WriteLine( "Base template (expected)." );
        }
    }
}

// <target>
[InheritedIntroduction]
internal class TargetClass { }