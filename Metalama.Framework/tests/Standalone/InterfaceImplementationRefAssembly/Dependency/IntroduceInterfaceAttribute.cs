// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Dependency;

public class IntroduceInterfaceAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Advice.ImplementInterface( builder.Target, typeof( IInterface ) );
    }

    [InterfaceMember]
    public int Property
    {
        get => 42;
        set { }
    }

    [InterfaceMember]
    public int Property_SetOnly
    {
        set { }
    }

    [InterfaceMember]
    public int Property_GetOnly => 42;

    [InterfaceMember]
    public int AutoProperty { get; set; }

    [InterfaceMember]
    public int AutoProperty_GetOnly { get; }

    [InterfaceMember]
    public void Method_Void() { }

    [InterfaceMember]
    public int Method_ValueType() => 42;

    [InterfaceMember]
    public object Method_NonNullable() => new object();

    [InterfaceMember]
    public object? Method_Nullable() => null;

    [InterfaceMember]
    public event EventHandler Event;

    [InterfaceMember]
    public event EventHandler EventField;

    [InterfaceMember]
    public event EventHandler? Event_Nullable;

    [InterfaceMember]
    public event EventHandler? EventField_Nullable;

    [InterfaceMember]
    public async Task AsyncMethod_Void() { }

    [InterfaceMember]
    public async Task<int> AsyncMethod_ValueType() => 42;

    [InterfaceMember]
    public async Task<object> AsyncMethod_NonNullable() => new object();

    [InterfaceMember]
    public async Task<object?> AsyncMethod_Nullable() => null;

    [InterfaceMember]
    public IEnumerable<int> IteratorMethod_ValueType()
    {
        yield return 42;
    }

    [InterfaceMember]
    public IEnumerable<object> IteratorMethod_NonNullable()
    {
        yield return new object();
    }

    [InterfaceMember]
    public IEnumerable<object?> IteratorMethod_Nullable()
    {
        yield return null;
    }

    [InterfaceMember]
    public async IAsyncEnumerable<int> AsyncIteratorMethod_ValueType()
    {
        yield return 42;
    }

    [InterfaceMember]
    public async IAsyncEnumerable<object> AsyncIteratorMethod_NonNullable()
    {
        yield return new object();
    }

    [InterfaceMember]
    public async IAsyncEnumerable<object?> AsyncIteratorMethod_Nullable()
    {
        yield return null;
    }
}