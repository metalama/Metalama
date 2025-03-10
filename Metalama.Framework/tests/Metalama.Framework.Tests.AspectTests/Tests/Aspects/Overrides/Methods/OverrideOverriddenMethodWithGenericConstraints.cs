// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.OverrideOverriddenMethodWithGenericConstraints;

public class MyAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Override" );

        // Call twice so we are sure it is not initialized.
        meta.Proceed();

        return meta.Proceed();
    }
}

public class BaseClass
{
    public virtual void M<T>( T t ) where T : IDisposable
    {
        t.Dispose();
    }
}

public class DerivedClass : BaseClass
{
    [MyAspect]

    // Generic parameters must not be repeated.
    public override void M<T>( T t )
    {
        t.Dispose();
    }
}