// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.DurableRef;

/// <summary>
/// Verifies that a durable reference to a type passed as a compile-time type argument of a template is resolved to the type.
/// </summary>
public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceMethod( nameof(Method), args: new { T = builder.Target.ToDurableRef() } );
    }

    [Template]
    private T? Method<[CompileTime] T>( T p, List<T> p2 ) where T : class
    {
        return p;
    }
}

// <target>
[Aspect]
public class Target { }
