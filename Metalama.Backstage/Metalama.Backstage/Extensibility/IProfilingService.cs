// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// Profiling service, which allows to control the current profiling session, if active.
/// </summary>
[PublicAPI]
public interface IProfilingService : IBackstageService 
{
    void Initialize();

    void CreateMemorySnapshot( string? snapshotName = null );
}