// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER && NET8_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_OverrideWithoutImplementation;

public class TheAspect : EventAspect
{
    public bool OverrideInvoke { get; set; }

    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.OverrideAccessors(
            nameof( Add ),
            nameof( Remove ),
            invokeTemplate: this.OverrideInvoke ? nameof( Invoke ) : null );
    }

    [Template]
    public void Add( dynamic handler ) => Console.WriteLine( "Add" );

    [Template]
    public void Remove( dynamic handler ) => Console.WriteLine( "Remove" );

    [Template]
    public dynamic? Invoke( dynamic handler )
    {
        Console.WriteLine( "Invoke" );

        return meta.Proceed();
    }
}

// <target>
internal partial class C
{
#if TESTRUNNER
    [TheAspect]
    public partial event EventHandler E1;
    
    [TheAspect(OverrideInvoke = true)]
    public partial event EventHandler E2;
#endif
}

#endif