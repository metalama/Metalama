// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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