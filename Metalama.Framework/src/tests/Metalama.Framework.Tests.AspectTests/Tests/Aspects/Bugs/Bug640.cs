// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug640;

/*
 * Tests that a ConstructorAspect applied to a primary constructor using [method:] attribute target
 * is correctly discovered and applied (#640).
 */

public class LogAttribute : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.Override( nameof(this.Template) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "Overridden." );
        meta.Proceed();
    }
}

#pragma warning disable CS9113 // Parameter is unread.

// <target>
[method: Log]
internal class C( int i )
{
    [Log]
    public C( string s ) : this( 0 ) { }
}