// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.IntroducedType;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.IntroduceClass( "IntroducedType" ).Declaration;

        builder.IntroduceMethod(
            nameof(Method),
            args: new { t = type },
            buildMethod: b => { b.Name = "FromBaseCompilation"; } );

        builder.IntroduceMethod(
            nameof(Method),
            args: new { t = type },
            buildMethod: b => { b.Name = "FromMutableCompilation"; } );
    }

    [Template]
    private void Method( [CompileTime] INamedType t )
    {
        Console.WriteLine( t.ToString() );
    }
}

// <target>
[Aspect]
public class Target { }