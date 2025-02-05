// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Telemetry;

[PublicAPI]
public interface IUsageReporter : IBackstageService
{
    bool IsUsageReportingEnabled { get; }

    bool ShouldReportSession( string projectName );

    IUsageSession? StartSession( string kind );
}