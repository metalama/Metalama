// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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