// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.HasNewProperty;

[Observable]
public partial class Base
{
    public int A { get; set; }
}

// <target>
public partial class HasNewProperty : Base
{
    public new int A { get; set; }
}