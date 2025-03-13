// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Parameter_CompileTime;

internal class Aspect : TypeAspect
{
    [Template]
    private void M()
    {
        LogMethod( null );
        LogString( "foo" );

        void LogMethod( IMethod? instance ) => Console.WriteLine( instance?.ToString() );

        void LogString( [CompileTime] string instance ) => Console.WriteLine( instance );
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(M) );
    }
}

// <target>
[Aspect]
internal class C { }