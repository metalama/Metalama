// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Observability.UnitTests.Assets.Core;

namespace Metalama.Patterns.Observability.UnitTests.Assets.IgnoreAutoChangePropertyAttribute;

[Observable]
public partial class SimpleWithIgnoreAutoChangeProperty
{
    public int P1 { get; set; }

    [NotObservable]
    public int P2 { get; set; }

    [NotObservable]
    public Simple P3 { get; set; }
}