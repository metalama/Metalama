// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.PrimaryConstructor_Record_Deconstruct;

/*
 * Tests that when a parameter is introduced into a record's primary constructor,
 * a Deconstruct overload for the original signature is generated so existing
 * deconstruction code remains valid (#698).
 */

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            if (constructor.IsRecordCopyConstructor())
            {
                continue;
            }

            builder.With( constructor ).IntroduceParameter( "introduced", typeof(int), TypedConstant.Create( 42 ) );
        }
    }
}

// <target>
[MyAspect]
public record R( int X, int Y )
{
    public void M()
    {
        // This deconstruction should still work after a parameter is introduced.
        var (x, y) = this;
    }
}
