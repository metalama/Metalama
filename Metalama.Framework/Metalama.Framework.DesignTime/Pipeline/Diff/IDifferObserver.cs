// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

/// <summary>
/// An observer interface used for testing this namespace.
/// </summary>
internal interface IDifferObserver : IGlobalService
{
    void OnNewCompilation();

    void OnComputeIncrementalChanges();

    void OnComputeNonIncrementalChanges();

    void OnMergeCompilationChanges();
}