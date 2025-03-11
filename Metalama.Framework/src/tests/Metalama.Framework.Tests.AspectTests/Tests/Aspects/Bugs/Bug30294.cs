// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30294;

internal class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        try
        {
            // This could not be in one line because of the bug.
            return meta.Proceed();
        }
        catch (Exception) when (meta.Target.Parameters[0].Value)
        {
            return default;
        }
    }
}

// <target>
internal class TestClass
{
    [TestAspect]
    private async void Execute( bool param )
    {
        await Task.CompletedTask;
    }
}