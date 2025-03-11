// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Observability.AspectTests.InheritFromAbstractBase;

public partial class C11 : C10
{
    /// <summary>
    /// Ref to <see cref="C10.C10P1"/>.
    /// </summary>
    public int C11P1 => this.C10P1;
}

[Observable]
public abstract partial class C10
{
    /// <summary>
    /// Auto
    /// </summary>
    public int C10P1 { get; set; }
}