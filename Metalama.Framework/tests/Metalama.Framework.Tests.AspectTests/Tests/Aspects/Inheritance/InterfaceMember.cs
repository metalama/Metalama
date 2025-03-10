// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.InterfaceMember;

[Inheritable]
internal class MyAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "overridden" );

        return meta.Proceed();
    }
}

internal interface IInterfaceA
{
    [MyAspect]
    public Task SomeMethodAsync();
}

internal interface IInterfaceB : IInterfaceA { }

// <target>
internal sealed class SomeImplementation : IInterfaceB
{
    public Task SomeMethodAsync()
    {
        return Task.CompletedTask;
    }
}