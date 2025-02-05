// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Dependency;

public interface IInterface
{
    int Property { get; set; }

    int Property_SetOnly { set; }

    int Property_GetOnly { get; }

    int AutoProperty { get; set; }

    int AutoProperty_GetOnly { get; }

    void Method_Void();

    int Method_ValueType();

    object Method_NonNullable();

    object? Method_Nullable();

    event EventHandler Event;

    event EventHandler EventField;

    Task AsyncMethod_Void();

    Task<int> AsyncMethod_ValueType();

    Task<object> AsyncMethod_NonNullable();

    Task<object?> AsyncMethod_Nullable();

    IEnumerable<int> IteratorMethod_ValueType();

    IEnumerable<object> IteratorMethod_NonNullable();

    IEnumerable<object?> IteratorMethod_Nullable();

    IAsyncEnumerable<int> AsyncIteratorMethod_ValueType();

    IAsyncEnumerable<object> AsyncIteratorMethod_NonNullable();

    IAsyncEnumerable<object?> AsyncIteratorMethod_Nullable();
}
