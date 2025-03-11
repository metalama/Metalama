// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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