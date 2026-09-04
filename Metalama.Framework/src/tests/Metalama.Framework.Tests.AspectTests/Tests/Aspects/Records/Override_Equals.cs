// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Override_Equals;

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var equals = builder.Target.Methods.OfName( "Equals" )
            .Single( m => m.Parameters[0].Type.Equals( builder.Target, TypeComparison.Default ) );

        builder.With( equals ).Override( nameof(Template) );
    }

    [Template]
    private dynamic? Template()
    {
        Console.WriteLine( "Overridden!" );

        return meta.Proceed();
    }
}

// <target>
[Override]
internal record Target( int X );
