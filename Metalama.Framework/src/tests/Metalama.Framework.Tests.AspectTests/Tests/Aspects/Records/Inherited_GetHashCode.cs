// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Inherited_GetHashCode;

[Inheritable]
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Methods.OfName( "GetHashCode" ).Single() ).Override( nameof(Template) );
    }

    [Template]
    private dynamic? Template()
    {
        Console.WriteLine( "Overridden!" );

        return meta.Proceed();
    }
}

// <target>
internal class Targets
{
    [Override]
    internal record BaseRecord( int X );

    internal record DerivedRecord( int X, int Y ) : BaseRecord( X );

    internal record TwiceDerivedRecord( int X, int Y, int Z ) : DerivedRecord( X, Y );
}
