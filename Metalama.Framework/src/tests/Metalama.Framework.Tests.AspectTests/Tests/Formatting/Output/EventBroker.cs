// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
// @LanguageVersion(12.0)
#endif

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Output.EventBroker;

internal class SafeEventAttribute : OverrideEventAspect
{
    public override dynamic? OverrideInvoke( dynamic? handler )
    {
        try
        {
            return meta.Proceed();
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );
            meta.Target.Event.Remove( handler );
            throw;
        }
    }
}

// <target>
internal class TargetCode
{
    [SafeEvent]
    public event EventHandler MyEvent
    {
        add { }
        remove { }
    }
}
