using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.InterfaceMember_Explicit;

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
    Task IInterfaceA.SomeMethodAsync()
    {
        return Task.CompletedTask;
    }
}