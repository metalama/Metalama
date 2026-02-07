// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.DeclarativeAbstract_InheritedTemplate;

// Tests that the [Template] attribute is inherited from a base aspect when overriding a property.
// When a base aspect declares an abstract [Template] property and a derived aspect overrides it
// without explicitly adding [Template], the override should still be recognized as a template.
// See https://github.com/metalama/Metalama/issues/821

public abstract class BaseAttribute : TypeAspect
{
    [Template]
    public abstract int Property { get; set; }
}

public class IntroductionAttribute : BaseAttribute
{
    public override int Property
    {
        get
        {
            Console.WriteLine( "Introduced property getter." );

            return 42;
        }
        set
        {
            Console.WriteLine( "Introduced property setter." );
        }
    }
}

// <target>
[Introduction]
internal class TargetClass { }
