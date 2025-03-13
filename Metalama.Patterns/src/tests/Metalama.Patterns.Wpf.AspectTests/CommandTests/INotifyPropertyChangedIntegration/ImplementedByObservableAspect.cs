// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// TODO: #34044 Can't use RequireOrderedAspects yet. If not fixed, re-enable when DependencyPropertyAttribute aspect is also ordered.
// __RequireOrderedAspects__

using Metalama.Patterns.Observability;

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.INotifyPropertyChangedIntegration.ImplementedByObservableAspect;

[Observable]
public class ImplementedByObservableAspect
{
    [Command]
    private void ExecuteFoo1() { }

    public bool CanExecuteFoo1 { get; set; }
}

public class ImplementedByBase : ImplementedByObservableAspect
{
    [Command]
    private void ExecuteFoo2() { }

    public bool CanExecuteFoo2 { get; set; }
}