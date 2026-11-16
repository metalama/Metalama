// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.DurableRef;

/// <summary>
/// Verifies that a durable reference passed as a compile-time template argument is resolved to the declaration that it references.
/// </summary>
public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceMethod( nameof(Method), args: new { f = builder.Target.Fields.OfName( "_value" ).Single().ToDurableRef() } );
    }

    [Template]
    private void Method( [CompileTime] IField f )
    {
        Console.WriteLine( meta.CompileTime( f.Name ) );
        Console.WriteLine( f.Value );
    }
}

// <target>
[Aspect]
public class Target
{
    private int _value;
}
