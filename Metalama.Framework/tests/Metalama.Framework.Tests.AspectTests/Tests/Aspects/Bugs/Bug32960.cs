// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32960;

public class ReportAndSwallowExceptionsAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        try
        {
            return meta.Proceed();
        }
        catch (Exception e)
        {
            Console.WriteLine( e );

            return default;
        }
    }
}

// <target>
public class PartProvider
{
    [ReportAndSwallowExceptions]
    public string GetPart( string name ) => throw new Exception( "This method has a bug." );
}