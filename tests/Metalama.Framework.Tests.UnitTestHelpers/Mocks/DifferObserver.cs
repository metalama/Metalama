// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Pipeline.Diff;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class DifferObserver : IDifferObserver
{
    public int NewCompilationEventCount { get; private set; }

    public int ComputeIncrementalChangesEventCount { get; private set; }

    public int ComputeNonIncrementalChangesEventCount { get; private set; }

    void IDifferObserver.OnNewCompilation() => this.NewCompilationEventCount++;

    void IDifferObserver.OnComputeIncrementalChanges() => this.ComputeIncrementalChangesEventCount++;

    void IDifferObserver.OnComputeNonIncrementalChanges() => this.ComputeNonIncrementalChangesEventCount++;

    public void OnMergeCompilationChanges() { }
}