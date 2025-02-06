// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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