// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics;

[Observable]
public partial class HasVirtualProperty
{
    public virtual int A { get; set; }
}