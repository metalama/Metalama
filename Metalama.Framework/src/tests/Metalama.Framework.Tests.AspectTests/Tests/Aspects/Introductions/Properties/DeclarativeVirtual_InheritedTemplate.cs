// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.DeclarativeVirtual_InheritedTemplate;

// Tests that the [Introduce] attribute is inherited from a base aspect when overriding a virtual property.
// When a base aspect declares a virtual [Introduce] property and a derived aspect overrides it
// without explicitly adding [Introduce], the override should still be recognized as a declarative introduction.
// See https://github.com/metalama/Metalama/issues/821

public abstract class BaseAttribute : TypeAspect
{
    [Introduce]
    public virtual int Property
    {
        get
        {
            Console.WriteLine( "Base property getter (wrong)." );

            return 0;
        }
        set
        {
            Console.WriteLine( "Base property setter (wrong)." );
        }
    }
}

public class IntroductionAttribute : BaseAttribute
{
    public override int Property
    {
        get
        {
            Console.WriteLine( "Overridden property getter." );

            return 42;
        }
        set
        {
            Console.WriteLine( "Overridden property setter." );
        }
    }
}

// <target>
[Introduction]
internal class TargetClass { }
