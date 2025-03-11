// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Metalama.Patterns.Observability.AspectTests.FieldBackedInpcProperty;

[Observable]
public class A
{
    public int A1 { get; set; }
}

// <target>
[Observable]
public class FieldBackedInpcProperty
{
    private A _x;

    public A P1 => this._x;

    public int P2 => this._x.A1;
}