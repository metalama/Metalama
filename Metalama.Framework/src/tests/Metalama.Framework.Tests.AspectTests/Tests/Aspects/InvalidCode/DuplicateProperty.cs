// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.DuplicateProperty;

/*
 * Tests that ambiguous declaration does not cause a crash in the linker. The output may not be correct.
 */

internal class Aspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty 
    { 
        get
        {
            Console.WriteLine( "Aspect" );

            return meta.Proceed();
        } 

        set
        {
            Console.WriteLine( "Aspect" );

            meta.Proceed();
        }
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private int Property { get; set; }

#if TESTRUNNER
    private int Property { get; set; }
#endif
}